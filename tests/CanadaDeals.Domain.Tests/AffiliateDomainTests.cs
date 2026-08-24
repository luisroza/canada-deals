using CanadaDeals.Domain.Affiliates;
using CanadaDeals.Domain.Common;

namespace CanadaDeals.Domain.Tests;

public sealed class AffiliateDomainTests
{
    [Fact]
    public void Active_program_requires_relationship_deeplink_and_domain_evidence()
    {
        Assert.Throws<InvalidOperationException>(() => AffiliateProgram.Create(
            Guid.NewGuid(), AffiliateProviderType.Impact, AffiliateProgramStatus.Active, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Pending_program_fails_closed_until_explicit_activation()
    {
        var now = DateTimeOffset.UtcNow;
        var program = AffiliateProgram.Create(Guid.NewGuid(), AffiliateProviderType.Impact,
            AffiliateProgramStatus.PendingApproval, now);

        Assert.False(program.CanGenerateLinks());

        program.Activate("program-1", "property-1", true, ["bestbuy.ca"], ["sjv.io"],
            "operator-evidence/reference", now.AddMinutes(1));

        Assert.True(program.CanGenerateLinks());
        Assert.Equal(AffiliateProgramStatus.Active, program.Status);
    }

    [Fact]
    public void Suspended_program_blocks_generation_without_deleting_existing_state()
    {
        var now = DateTimeOffset.UtcNow;
        var program = AffiliateProgram.Create(Guid.NewGuid(), AffiliateProviderType.Cj,
            AffiliateProgramStatus.Active, now, "advertiser", "website", "link", true,
            ["homedepot.ca"], ["tkqlhce.com"], "joined-evidence", now);

        program.SetStatus(AffiliateProgramStatus.Suspended, now.AddHours(1));

        Assert.False(program.CanGenerateLinks());
        Assert.Equal("advertiser", program.ProviderProgramId);
    }

    [Fact]
    public void Affiliate_link_expiry_is_independent_from_product_truth()
    {
        var now = DateTimeOffset.UtcNow;
        var link = AffiliateLink.CreateActive(Guid.NewGuid(), Guid.NewGuid(), AffiliateProviderType.Impact,
            "https://example.sjv.io/c/1", "https://bestbuy.ca/product/1", now, now.AddHours(1), now.AddDays(1));

        Assert.True(link.IsUsable(now.AddHours(12)));
        Assert.False(link.IsUsable(now.AddDays(2)));
        Assert.DoesNotContain(typeof(AffiliateLink).GetProperties(), property =>
            property.Name.Contains("Commission", StringComparison.OrdinalIgnoreCase) ||
            property.Name.Contains("Epc", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Store_destination_has_its_own_lifecycle_without_listing_identity_or_economics()
    {
        var now = DateTimeOffset.UtcNow;
        var destination = StoreAffiliateDestination.CreateActive(
            Guid.NewGuid(), Guid.NewGuid(), AffiliateProviderType.Rakuten,
            "https://tracking.safe.test/store", "https://merchant.safe.test/", now, now.AddDays(1), now.AddDays(7));

        Assert.True(destination.IsUsable(now.AddDays(2)));
        Assert.False(destination.IsUsable(now.AddDays(8)));
        Assert.DoesNotContain(typeof(StoreAffiliateDestination).GetProperties(), property =>
            property.Name.Contains("Listing", StringComparison.OrdinalIgnoreCase) ||
            property.Name.Contains("Commission", StringComparison.OrdinalIgnoreCase) ||
            property.Name.Contains("Epc", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Store_banner_original_art_keeps_brand_rights_unknown_and_can_be_disabled()
    {
        var profile = CanadaDeals.Domain.Retailers.StoreBannerProfile.CreateOriginal(
            Guid.NewGuid(), "Shop technology", "Browse current offers", "/store-banners/pc-hardware.svg", 10);

        Assert.Equal(PolicyPermission.Unknown, profile.BrandAssetPolicy);
        Assert.Equal(StoreBannerAssetSource.CanadaDealsOriginal, profile.AssetSource);
        Assert.True(profile.IsDisplayable(DateTimeOffset.UtcNow));
        Assert.True(profile.CanUseConfiguredAsset(DateTimeOffset.UtcNow));

        profile.Disable();

        Assert.False(profile.IsDisplayable(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Merchant_approved_store_art_requires_provider_evidence_placement_and_current_rights()
    {
        var now = DateTimeOffset.UtcNow;
        var profile = CanadaDeals.Domain.Retailers.StoreBannerProfile.CreateMerchantApproved(
            Guid.NewGuid(), AffiliateProviderType.Impact, "Shop technology", "Browse current offers",
            "/store-banners/pc-hardware.svg", 10, "approved-creative-evidence", "store_banner",
            now.AddDays(-1), now.AddDays(1));

        Assert.Equal(PolicyPermission.Allowed, profile.BrandAssetPolicy);
        Assert.Equal(StoreBannerAssetSource.MerchantApprovedAffiliateAsset, profile.AssetSource);
        Assert.Equal(AffiliateProviderType.Impact, profile.AssetProvider);
        Assert.True(profile.CanUseConfiguredAsset(now));
        Assert.False(profile.CanUseConfiguredAsset(now.AddDays(2)));
        Assert.Throws<ArgumentException>(() => CanadaDeals.Domain.Retailers.StoreBannerProfile.CreateMerchantApproved(
            Guid.NewGuid(), AffiliateProviderType.Impact, "Shop technology", "Browse current offers",
            "/store-banners/pc-hardware.svg", 10, "approved-creative-evidence", "homepage_hero",
            now.AddDays(-1), now.AddDays(1)));
    }

    [Fact]
    public void Store_click_contains_only_controlled_store_context()
    {
        var click = ClickEvent.CreateForStore(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "store_banner", DateTimeOffset.UtcNow);

        Assert.Null(click.AffiliateLinkId);
        Assert.Null(click.RetailerListingId);
        Assert.NotNull(click.StoreAffiliateDestinationId);
        Assert.Equal("store_banner", click.Placement);
    }
}
