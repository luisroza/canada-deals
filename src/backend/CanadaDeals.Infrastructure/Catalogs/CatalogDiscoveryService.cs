using CanadaDeals.Domain.Integrations;
using CanadaDeals.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CanadaDeals.Infrastructure.Catalogs;

public sealed class CatalogDiscoveryService(DealsDbContext db, IEnumerable<IOfferCatalogSource> sources, TimeProvider clock)
{
    public async Task<IReadOnlyList<CatalogCandidate>> DiscoverAsync(
        string provider,
        bool persistSnapshot,
        int maximumCandidates = 100,
        CancellationToken cancellationToken = default)
    {
        var source = sources.SingleOrDefault(candidate => string.Equals(candidate.Provider, provider, StringComparison.OrdinalIgnoreCase))
            ?? throw new CatalogProviderException(CatalogFailureKind.Configuration, "CATALOG_PROVIDER_NOT_REGISTERED");
        var limit = Math.Clamp(maximumCandidates, 1, 500);
        var candidates = (await source.DiscoverAsync(new CatalogDiscoveryRequest(limit), cancellationToken))
            .Take(limit)
            .ToArray();
        if (candidates.Any(candidate => !string.Equals(candidate.Provider, source.Provider, StringComparison.OrdinalIgnoreCase)))
            throw new CatalogProviderException(CatalogFailureKind.MalformedResponse, "CATALOG_DISCOVERY_PROVIDER_MISMATCH");
        if (!persistSnapshot) return candidates;

        var now = clock.GetUtcNow();
        foreach (var candidate in candidates)
        {
            var existing = await db.CatalogMerchantSources.SingleOrDefaultAsync(source =>
                source.Provider == candidate.Provider.ToLowerInvariant() &&
                source.ProviderAdvertiserId == candidate.ProviderAdvertiserId &&
                source.CatalogId == (candidate.CatalogId ?? string.Empty), cancellationToken);
            try
            {
                if (existing is null)
                {
                    db.CatalogMerchantSources.Add(CatalogMerchantSource.CreateDiscovery(candidate.Provider,
                        candidate.ProviderAdvertiserId, candidate.CatalogId, candidate.DisplayName,
                        candidate.RelationshipStatus, candidate.CatalogAvailable, candidate.AffiliateAvailable,
                        candidate.CanadaRelevant, candidate.Currency, now));
                }
                else
                {
                    existing.ReconcileDiscovery(candidate.DisplayName, candidate.RelationshipStatus,
                        candidate.CatalogAvailable, candidate.AffiliateAvailable, candidate.CanadaRelevant,
                        candidate.Currency, now);
                }
            }
            catch (ArgumentException exception)
            {
                throw new CatalogProviderException(CatalogFailureKind.MalformedResponse,
                    "CATALOG_DISCOVERY_CANDIDATE_INVALID", innerException: exception);
            }
        }
        await db.SaveChangesAsync(cancellationToken);
        return candidates;
    }
}
