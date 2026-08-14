using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.Integrations;
using CanadaDeals.Domain.Policies;

namespace CanadaDeals.Domain.Tests;

public sealed class RakutenDomainTests
{
    [Fact]
    public void Active_capabilities_still_require_operator_mapping_and_policy()
    {
        var now = DateTimeOffset.UtcNow;
        var capability = Capability(now);
        var policy = Policy(PolicyPermission.Allowed, PolicyPermission.Allowed, PolicyPermission.Allowed, now);

        Assert.False(capability.CanGenerateAffiliateLink(policy));
        Assert.False(capability.CanPersistCatalog(policy));

        capability.ConfigureOperatorMapping(Guid.NewGuid(), policy.Id, true, true, true, now);

        Assert.True(capability.CanGenerateAffiliateLink(policy));
        Assert.True(capability.CanPersistCatalog(policy));
    }

    [Fact]
    public void Unknown_policy_fails_closed_for_affiliate_and_catalog()
    {
        var now = DateTimeOffset.UtcNow;
        var capability = Capability(now);
        var policy = Policy(PolicyPermission.Unknown, PolicyPermission.Unknown, PolicyPermission.Unknown, now);
        capability.ConfigureOperatorMapping(Guid.NewGuid(), policy.Id, true, true, true, now);

        Assert.False(capability.CanGenerateAffiliateLink(policy));
        Assert.False(capability.CanPersistCatalog(policy));
    }

    [Fact]
    public void Partnership_reconciliation_disables_activation()
    {
        var now = DateTimeOffset.UtcNow;
        var capability = Capability(now);
        var policy = Policy(PolicyPermission.Allowed, PolicyPermission.Allowed, PolicyPermission.Allowed, now);
        capability.ConfigureOperatorMapping(Guid.NewGuid(), policy.Id, true, true, true, now);

        capability.ReconcileProviderSnapshot("Merchant", "https://merchant.test", IntegrationAdvertiserStatus.Active,
            IntegrationPartnershipStatus.TemporaryRemove, ["CA"], true, true, now.AddHours(1), now, now.AddHours(1));

        Assert.False(capability.AffiliateEnabled);
        Assert.False(capability.CatalogEnabled);
    }

    [Fact]
    public void Missing_provider_snapshot_fails_closed_and_removes_operator_activation()
    {
        var now = DateTimeOffset.UtcNow;
        var capability = Capability(now);
        var policy = Policy(PolicyPermission.Allowed, PolicyPermission.Allowed, PolicyPermission.Allowed, now);
        capability.ConfigureOperatorMapping(Guid.NewGuid(), policy.Id, true, true, true, now);

        capability.MarkProviderUnavailable(now.AddHours(1));

        Assert.Equal(IntegrationAdvertiserStatus.Unknown, capability.AdvertiserStatus);
        Assert.Equal(IntegrationPartnershipStatus.Unknown, capability.PartnershipStatus);
        Assert.False(capability.AffiliateEnabled);
        Assert.False(capability.CatalogEnabled);
        Assert.False(capability.CanGenerateAffiliateLink(policy));
        Assert.False(capability.CanPersistCatalog(policy));
    }

    private static RakutenAdvertiserCapability Capability(DateTimeOffset now) => RakutenAdvertiserCapability.Create(
        "101", "Merchant", "https://merchant.test", IntegrationAdvertiserStatus.Active,
        IntegrationPartnershipStatus.Active, ["CA"], true, true, now, now, now);

    private static MerchantPolicy Policy(PolicyPermission price, PolicyPermission metadata, PolicyPermission affiliate, DateTimeOffset now) =>
        MerchantPolicy.Create($"policy-{Guid.NewGuid():N}", price, PolicyPermission.Unknown, PolicyPermission.Denied,
            metadata, 24, "SAME_PRODUCT_ONLY", "RAKUTEN", "Disclosure", 0, "Unknown", now, affiliate);
}
