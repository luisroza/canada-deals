using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.Integrations;
using CanadaDeals.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CanadaDeals.Infrastructure.Rakuten;

public sealed class RakutenDiscoveryService(
    IRakutenPartnershipClient partnerships,
    IRakutenAdvertiserClient advertisers,
    DealsDbContext db,
    IOptions<RakutenOptions> options,
    TimeProvider clock)
{
    private readonly RakutenOptions _options = options.Value;

    public async Task<RakutenDiscoveryResult> DiscoverAsync(bool persistCapabilities, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || !_options.LiveDiscoveryEnabled)
            throw new RakutenProviderException(RakutenFailureKind.ConfigurationError, "RAKUTEN_LIVE_DISCOVERY_DISABLED");

        var partnershipRows = await partnerships.GetAllAsync(cancellationToken);
        var advertiserRows = await advertisers.GetAllAsync(cancellationToken);
        var advertiserByMid = advertiserRows.GroupBy(row => row.Mid).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var capabilities = partnershipRows
            .Where(partnership => advertiserByMid.ContainsKey(partnership.AdvertiserMid))
            .Select(partnership =>
            {
                var advertiser = advertiserByMid[partnership.AdvertiserMid];
                return new RakutenCapabilityRecord(advertiser, partnership, IsCanadaRelevant(advertiser));
            })
            .OrderBy(capability => capability.Advertiser.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (persistCapabilities)
        {
            var existing = await db.RakutenAdvertiserCapabilities
                .ToDictionaryAsync(capability => capability.AdvertiserMid, StringComparer.Ordinal, cancellationToken);
            var now = clock.GetUtcNow();
            var seenMids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var capability in capabilities)
            {
                seenMids.Add(capability.Advertiser.Mid);
                if (existing.TryGetValue(capability.Advertiser.Mid, out var stored))
                {
                    stored.ReconcileProviderSnapshot(
                        capability.Advertiser.Name, capability.Advertiser.Url, capability.Partnership.AdvertiserStatus,
                        capability.Partnership.PartnershipStatus, capability.Advertiser.ShipsTo,
                        capability.Advertiser.ProductFeedAvailable, capability.Advertiser.DeepLinksAvailable, now,
                        capability.Partnership.ApprovedAt, capability.Partnership.StatusUpdatedAt);
                }
                else
                {
                    var createdCapability = RakutenAdvertiserCapability.Create(
                        capability.Advertiser.Mid, capability.Advertiser.Name, capability.Advertiser.Url,
                        capability.Partnership.AdvertiserStatus, capability.Partnership.PartnershipStatus,
                        capability.Advertiser.ShipsTo, capability.Advertiser.ProductFeedAvailable,
                        capability.Advertiser.DeepLinksAvailable, now, capability.Partnership.ApprovedAt,
                        capability.Partnership.StatusUpdatedAt);
                    db.RakutenAdvertiserCapabilities.Add(createdCapability);
                    existing.Add(createdCapability.AdvertiserMid, createdCapability);
                }
            }

            foreach (var missing in existing.Values.Where(candidate => !seenMids.Contains(candidate.AdvertiserMid)))
                missing.MarkProviderUnavailable(now);

            var ineligibleMids = existing.Values
                .Where(candidate => !candidate.CanProviderEnableAffiliate())
                .Select(candidate => candidate.AdvertiserMid)
                .ToArray();
            if (ineligibleMids.Length > 0)
            {
                var programs = await db.AffiliatePrograms
                    .Where(program => program.Provider == AffiliateProviderType.Rakuten &&
                                      program.ProviderProgramId != null &&
                                      ineligibleMids.Contains(program.ProviderProgramId))
                    .ToListAsync(cancellationToken);
                foreach (var program in programs.Where(program => program.Status == AffiliateProgramStatus.Active))
                    program.SetStatus(AffiliateProgramStatus.Suspended, now);

                var programIds = programs.Select(program => program.Id).ToArray();
                if (programIds.Length > 0)
                {
                    var links = await db.AffiliateLinks
                        .Where(link => programIds.Contains(link.AffiliateProgramId) && link.Status == AffiliateLinkStatus.Active)
                        .ToListAsync(cancellationToken);
                    foreach (var link in links) link.Disable("RAKUTEN_RELATIONSHIP_INACTIVE");
                }
            }
            await db.SaveChangesAsync(cancellationToken);
        }

        var active = capabilities.Where(IsActive).ToList();
        return new RakutenDiscoveryResult(
            capabilities, advertiserRows.Count, partnershipRows.Count, active.Count,
            active.Count(capability => capability.CanadaRelevant),
            active.Count(capability => capability.CanadaRelevant && capability.Advertiser.ProductFeedAvailable),
            active.Count(capability => capability.CanadaRelevant && capability.Advertiser.DeepLinksAvailable));
    }

    private static bool IsActive(RakutenCapabilityRecord capability) =>
        capability.Partnership.PartnershipStatus == IntegrationPartnershipStatus.Active &&
        capability.Partnership.AdvertiserStatus == IntegrationAdvertiserStatus.Active;

    private static bool IsCanadaRelevant(RakutenAdvertiserRecord advertiser) =>
        advertiser.ShipsTo.Any(country => country.Equals("CA", StringComparison.OrdinalIgnoreCase) ||
                                          country.Equals("CAN", StringComparison.OrdinalIgnoreCase) ||
                                          country.Equals("CANADA", StringComparison.OrdinalIgnoreCase));
}
