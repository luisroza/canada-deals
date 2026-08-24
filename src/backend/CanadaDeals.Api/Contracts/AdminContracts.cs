using System.ComponentModel.DataAnnotations;

namespace CanadaDeals.Api.Contracts;

public sealed record AdminSessionResponse(bool IsAuthenticated, bool IsAdmin, string? Email);

public sealed record AdminReferenceOption(Guid Id, string Key, string Label, bool IsEnabled = true);

public sealed record AdminPolicyOption(
    Guid Id,
    string SourceKey,
    string PriceStorage,
    string PriceHistory,
    string AffiliateLinks,
    string RequiredAttribution);

public sealed record AdminDashboardCounts(
    int PublishedOffers,
    int DraftOffers,
    int EnabledBanners,
    int BlockedOrExpiredBanners,
    int OpenReports);

public sealed record AdminOfferResponse(
    Guid ListingId,
    Guid ProductId,
    string Slug,
    string ProductTitle,
    Guid BrandId,
    string Brand,
    Guid CategoryId,
    string Category,
    string? ModelNumber,
    string? ManufacturerPartNumber,
    string? Gtin,
    IReadOnlyDictionary<string, string> VariantAttributes,
    Guid RetailerId,
    string Retailer,
    Guid MerchantPolicyId,
    string MerchantPolicy,
    string ExternalListingId,
    string? RetailerSku,
    string OriginalTitle,
    string ProductUrl,
    string? ApprovedAffiliateDestinationReference,
    string? Seller,
    bool? IsMarketplaceSeller,
    string ConditionState,
    int? PackQuantity,
    string? BundleContents,
    string? RegionAvailabilityContext,
    string AvailabilityState,
    string? ShippingContext,
    IReadOnlyDictionary<string, string> ExternalIdentifiers,
    DateTimeOffset? ObservedAt,
    DateTimeOffset? FetchedAt,
    decimal? CurrentPrice,
    string Currency,
    string MatchState,
    string EvidenceState,
    string HistoryState,
    bool IsEnabled,
    bool IsPubliclyEligible,
    string ReadinessSummary,
    string PreviewPath);

public sealed record AdminBannerResponse(
    Guid RetailerId,
    string RetailerKey,
    string Retailer,
    Guid? ProfileId,
    string Title,
    string Subtitle,
    string? AssetPath,
    string AssetSource,
    string BrandAssetPolicy,
    string? AssetProvider,
    string? AllowedPlacement,
    int BannerOrder,
    bool IsEnabled,
    string? AssetEvidenceReference,
    DateTimeOffset? EffectiveAt,
    DateTimeOffset? ExpiresAt,
    string VisibilityState,
    string RightsState);

public sealed record AdminAuditResponse(
    Guid Id,
    string Action,
    string EntityType,
    Guid EntityId,
    string Summary,
    DateTimeOffset CreatedAt);

public sealed record AdminReportResponse(
    Guid ReportId,
    Guid ListingId,
    string Retailer,
    string ListingTitle,
    string Reason,
    string? CustomerNote,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record UpdateAdminReportRequest(
    [Required, MaxLength(30)] string Status,
    [Required, MaxLength(300)] string ResolutionNote);

public sealed record AdminDashboardResponse(
    AdminDashboardCounts Counts,
    IReadOnlyList<AdminReferenceOption> Brands,
    IReadOnlyList<AdminReferenceOption> Categories,
    IReadOnlyList<AdminReferenceOption> Retailers,
    IReadOnlyList<AdminPolicyOption> Policies,
    IReadOnlyList<AdminOfferResponse> Offers,
    IReadOnlyList<AdminBannerResponse> Banners,
    IReadOnlyList<AdminReportResponse> Reports,
    IReadOnlyList<AdminAuditResponse> RecentAudit);

public sealed record UpsertAdminOfferRequest(
    [Required, MaxLength(140)] string Slug,
    [Required, MaxLength(240)] string ProductTitle,
    Guid BrandId,
    Guid CategoryId,
    [MaxLength(120)] string? ModelNumber,
    [MaxLength(120)] string? ManufacturerPartNumber,
    [MaxLength(32)] string? Gtin,
    IReadOnlyDictionary<string, string>? VariantAttributes,
    Guid RetailerId,
    Guid MerchantPolicyId,
    [Required, MaxLength(160)] string ExternalListingId,
    [MaxLength(160)] string? RetailerSku,
    [Required, MaxLength(300)] string OriginalTitle,
    [Required, MaxLength(1000)] string ProductUrl,
    [MaxLength(1000)] string? ApprovedAffiliateDestinationReference,
    [MaxLength(240)] string? Seller,
    bool? IsMarketplaceSeller,
    [Required, MaxLength(30)] string ConditionState,
    [Range(1, 1000)] int? PackQuantity,
    [MaxLength(500)] string? BundleContents,
    [MaxLength(240)] string? RegionAvailabilityContext,
    [Required, MaxLength(30)] string AvailabilityState,
    [MaxLength(500)] string? ShippingContext,
    IReadOnlyDictionary<string, string>? ExternalIdentifiers,
    [Range(typeof(decimal), "0.01", "1000000")] decimal CurrentPrice,
    DateTimeOffset ObservedAt,
    DateTimeOffset FetchedAt,
    [Required, MaxLength(40)] string MatchState,
    bool IsEnabled,
    [MaxLength(300)] string? ChangeReason);

public sealed record UpsertAdminBannerRequest(
    [Required, MaxLength(120)] string Title,
    [Required, MaxLength(180)] string Subtitle,
    [MaxLength(300)] string? AssetPath,
    [Required, MaxLength(60)] string AssetSource,
    [MaxLength(40)] string? AssetProvider,
    [MaxLength(500)] string? AssetEvidenceReference,
    [MaxLength(80)] string? AllowedPlacement,
    DateTimeOffset? EffectiveAt,
    DateTimeOffset? ExpiresAt,
    [Range(0, 10000)] int BannerOrder,
    bool IsEnabled,
    [MaxLength(300)] string? ChangeReason);
