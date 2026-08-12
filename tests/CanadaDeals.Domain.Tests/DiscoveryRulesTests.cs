using CanadaDeals.Domain.Search;

namespace CanadaDeals.Domain.Tests;

public sealed class DiscoveryRulesTests
{
    [Theory]
    [InlineData("NS55QLED-2026", "NS55QLED2026")]
    [InlineData(" mf-20 kit ", "MF20KIT")]
    [InlineData("0000 0000-0001", "000000000001")]
    public void Identifier_normalization_is_case_and_punctuation_tolerant(string value, string expected) =>
        Assert.Equal(expected, DiscoveryRules.NormalizeIdentifier(value));

    [Fact]
    public void General_feed_defaults_to_recent_and_search_defaults_to_relevance()
    {
        Assert.Equal(DiscoverySort.RecentlyChecked, DiscoveryRules.DefaultSort(null));
        Assert.Equal(DiscoverySort.Relevance, DiscoveryRules.DefaultSort("television"));
    }

    [Theory]
    [InlineData(100, 120, true)]
    [InlineData(120, 100, false)]
    [InlineData(100, null, false)]
    public void Savings_requires_a_higher_supported_reference(int current, int? reference, bool expected) =>
        Assert.Equal(expected, DiscoveryRules.SupportedSavings(current, reference));

    [Fact]
    public void Sort_contract_contains_only_explainable_discovery_signals()
    {
        var publicNames = typeof(DiscoveryRules).GetMembers().Select(member => member.Name).ToArray();
        Assert.DoesNotContain(publicNames, name => name.Contains("Commission", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(publicNames, name => name.Contains("Affiliate", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(publicNames, name => name.Contains("User", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(new[] { "Relevance", "RecentlyChecked", "SupportedSavings", "LowestPrice" }, Enum.GetNames<DiscoverySort>());
    }
}
