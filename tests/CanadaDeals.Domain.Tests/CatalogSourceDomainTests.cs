using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.Integrations;
using CanadaDeals.Domain.Policies;

namespace CanadaDeals.Domain.Tests;

public sealed class CatalogSourceDomainTests
{
    [Fact]
    public void Provider_name_is_normalized_but_provider_advertiser_identity_preserves_case()
    {
        var source = CatalogMerchantSource.CreateDiscovery(
            "EBAY", "EBAY_CA", null, "eBay Canada", IntegrationPartnershipStatus.Active,
            true, false, true, "CAD", DateTimeOffset.Parse("2026-09-03T00:00:00Z"));

        Assert.Equal("ebay", source.Provider);
        Assert.Equal("EBAY_CA", source.ProviderAdvertiserId);
        Assert.Throws<ArgumentException>(() => CatalogMerchantSource.CreateDiscovery(
            new string('p', 25), "advertiser", null, "Provider", IntegrationPartnershipStatus.Active,
            true, false, true, "CAD", DateTimeOffset.Parse("2026-09-03T00:00:00Z")));
    }

    [Fact]
    public void Source_starts_unmapped_and_requires_explicit_mapping_hosts_before_activation()
    {
        var now = DateTimeOffset.Parse("2026-09-01T00:00:00Z");
        var source = Source(now);

        Assert.Equal(CatalogSourceState.Unmapped, source.State);
        Assert.Throws<InvalidOperationException>(() => source.ConfigureMapping(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), [], true, now));
    }

    [Fact]
    public void Active_relationship_and_complete_disabled_mapping_is_ready_for_dry_run()
    {
        var now = DateTimeOffset.Parse("2026-09-01T00:00:00Z");
        var source = Source(now);

        source.ConfigureMapping(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ["retailer.example"], false, now);

        Assert.Equal(CatalogSourceState.ReadyForDryRun, source.State);
        Assert.False(source.CatalogEnabled);
        Assert.Equal(["retailer.example"], source.AllowedDestinationHosts);
    }

    [Fact]
    public void Unknown_policy_blocks_persistence_even_when_provider_and_operator_gates_are_active()
    {
        var now = DateTimeOffset.Parse("2026-09-01T00:00:00Z");
        var policy = MerchantPolicy.Create("controlled", PolicyPermission.Unknown, PolicyPermission.Unknown,
            PolicyPermission.Unknown, PolicyPermission.Unknown, null, "UNKNOWN", "UNKNOWN", string.Empty,
            null, string.Empty, now, PolicyPermission.Unknown);
        var source = Source(now);
        source.ConfigureMapping(Guid.NewGuid(), policy.Id, Guid.NewGuid(), ["retailer.example"], true, now);

        Assert.Equal(CatalogSourceState.Active, source.State);
        Assert.False(source.CanPersist(policy));
    }

    [Fact]
    public void Provider_relationship_loss_disables_catalog_and_fails_closed()
    {
        var now = DateTimeOffset.Parse("2026-09-01T00:00:00Z");
        var source = Source(now);
        source.ConfigureMapping(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ["retailer.example"], true, now);

        source.ReconcileDiscovery("Controlled", IntegrationPartnershipStatus.TemporaryRemove,
            true, false, true, "CAD", now.AddHours(1));

        Assert.False(source.CatalogEnabled);
        Assert.Equal(CatalogSourceState.MappedPolicyBlocked, source.State);
    }

    private static CatalogMerchantSource Source(DateTimeOffset now) => CatalogMerchantSource.CreateDiscovery(
        "impact", "advertiser", "catalog", "Controlled", IntegrationPartnershipStatus.Active,
        true, false, true, "CAD", now);
}
