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
}
