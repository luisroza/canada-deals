using CanadaDeals.Api.Contracts;
using CanadaDeals.Domain.Affiliates;
using CanadaDeals.Domain.Common;
using CanadaDeals.Infrastructure.Affiliates;
using CanadaDeals.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CanadaDeals.Api.Services;

public sealed class StoreBannerQueryService(DealsDbContext db, TimeProvider clock)
{
    private const string TestOnlyAttribution = "TEST_ONLY";
    private const string FallbackAsset = "/store-banners/marketplace-packages.svg";

    public async Task<IReadOnlyList<StoreBannerResponse>> GetAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var retailers = await db.Retailers.AsNoTracking()
            .Where(retailer => retailer.IsEnabled && db.RetailerListings.Any(listing =>
                listing.IsEnabled && listing.RetailerId == retailer.Id &&
                listing.MerchantPolicy.AllowPriceStorage == PolicyPermission.Allowed &&
                listing.MerchantPolicy.RequiredAttribution != TestOnlyAttribution))
            .OrderBy(retailer => retailer.Name)
            .ToListAsync(cancellationToken);
        if (retailers.Count == 0) return [];

        var retailerIds = retailers.Select(retailer => retailer.Id).ToArray();
        var profiles = await db.StoreBannerProfiles.AsNoTracking()
            .Where(profile => retailerIds.Contains(profile.RetailerId))
            .ToDictionaryAsync(profile => profile.RetailerId, cancellationToken);
        var affiliateAllowed = await db.RetailerListings.AsNoTracking()
            .Where(listing => listing.IsEnabled && retailerIds.Contains(listing.RetailerId) &&
                              listing.MerchantPolicy.AllowAffiliateLinks == PolicyPermission.Allowed)
            .Select(listing => listing.RetailerId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var destinations = await db.StoreAffiliateDestinations.AsNoTracking()
            .Include(destination => destination.AffiliateProgram)
            .Where(destination => retailerIds.Contains(destination.RetailerId) &&
                                  destination.Status == AffiliateLinkStatus.Active)
            .OrderByDescending(destination => destination.LastValidatedAt)
            .ToListAsync(cancellationToken);
        var rakutenCapabilities = await db.RakutenAdvertiserCapabilities.AsNoTracking()
            .Include(capability => capability.MerchantPolicy)
            .Where(capability => capability.RetailerId != null && retailerIds.Contains(capability.RetailerId.Value))
            .ToListAsync(cancellationToken);

        var allowedRetailerIds = affiliateAllowed.ToHashSet();
        var result = new List<(int Order, string Name, StoreBannerResponse Banner)>();
        foreach (var retailer in retailers)
        {
            profiles.TryGetValue(retailer.Id, out var profile);
            if (profile is not { } configuredProfile || !configuredProfile.IsDisplayable(now)) continue;

            var destination = destinations.FirstOrDefault(candidate =>
                candidate.RetailerId == retailer.Id &&
                candidate.IsUsable(now) &&
                candidate.AffiliateProgram.RetailerId == retailer.Id &&
                candidate.AffiliateProgram.Provider == candidate.Provider &&
                candidate.AffiliateProgram.CanGenerateLinks() &&
                AffiliateUrlPolicy.TryValidateHttps(candidate.DestinationUrl, candidate.AffiliateProgram.DestinationDomains, out _) &&
                AffiliateUrlPolicy.TryValidateHttps(candidate.TrackingUrl, candidate.AffiliateProgram.TrackingDomains, out _) &&
                (candidate.Provider != AffiliateProviderType.Rakuten || rakutenCapabilities.Any(capability =>
                    capability.RetailerId == retailer.Id &&
                    capability.AdvertiserMid == candidate.AffiliateProgram.ProviderProgramId &&
                    capability.MerchantPolicy is not null &&
                    capability.CanGenerateAffiliateLink(capability.MerchantPolicy))));
            var active = allowedRetailerIds.Contains(retailer.Id) && destination is not null;
            var usesConfiguredAsset = configuredProfile.CanUseConfiguredAsset(now);
            var assetPath = usesConfiguredAsset ? configuredProfile.AssetPath! : FallbackAsset;
            var assetSource = usesConfiguredAsset
                ? configuredProfile.AssetSource
                : StoreBannerAssetSource.CanadaDealsOriginal;
            var brandAssetPolicy = usesConfiguredAsset && assetSource == StoreBannerAssetSource.MerchantApprovedAffiliateAsset
                ? configuredProfile.BrandAssetPolicy
                : PolicyPermission.Unknown;

            var escapedRetailerKey = Uri.EscapeDataString(retailer.Key);

            result.Add((
                configuredProfile.BannerOrder,
                retailer.Name,
                new StoreBannerResponse(
                    retailer.Key,
                    retailer.Name,
                    configuredProfile.Title,
                    configuredProfile.Subtitle,
                    assetPath,
                    assetSource.ToString().ToUpperInvariant(),
                    brandAssetPolicy.ToString().ToUpperInvariant(),
                    active ? "ACTIVE_AFFILIATE" : "DISCOVERY_ONLY",
                    active ? $"/go/store/{escapedRetailerKey}" : $"/?retailer={escapedRetailerKey}#deals",
                    active)));
        }

        return result.OrderBy(item => item.Order).ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(item => item.Banner).ToList();
    }
}
