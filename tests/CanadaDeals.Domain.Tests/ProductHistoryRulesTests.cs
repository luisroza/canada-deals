using CanadaDeals.Domain.PriceTruth;

namespace CanadaDeals.Domain.Tests;

public sealed class ProductHistoryRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(null, ProductHistoryWindow.ThirtyDays)]
    [InlineData("30d", ProductHistoryWindow.ThirtyDays)]
    [InlineData("90D", ProductHistoryWindow.NinetyDays)]
    public void Parses_only_bounded_approved_windows(string? value, ProductHistoryWindow expected)
    {
        Assert.True(ProductHistoryRules.TryParseWindow(value, out var actual));
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Rejects_unbounded_or_unsupported_window() =>
        Assert.False(ProductHistoryRules.TryParseWindow("all", out _));

    [Fact]
    public void Thirty_day_window_excludes_older_future_and_unsupported_currency_observations()
    {
        var result = ProductHistoryRules.Evaluate(ProductHistoryWindow.ThirtyDays, Now,
        [
            Observation(29, 120), Observation(5, 100), Observation(31, 50),
            new(75, "USD", Now.AddDays(-2)), new(1, "CAD", Now.AddDays(1))
        ], Now.AddDays(-31));

        Assert.Equal(2, result.ObservationCount);
        Assert.Equal(100m, result.LowestObservedPrice);
        Assert.Equal(Now.AddDays(-31), result.TrackingStart);
    }

    [Fact]
    public void Ninety_day_window_includes_older_permitted_evidence()
    {
        var result = ProductHistoryRules.Evaluate(ProductHistoryWindow.NinetyDays, Now,
            [Observation(70, 130), Observation(5, 100)], Now.AddDays(-70));

        Assert.Equal(2, result.ObservationCount);
        Assert.Equal(Now.AddDays(-70).Date, result.ObservationStart!.Value.Date);
    }

    [Fact]
    public void Daily_projection_uses_lowest_real_observation_without_fabricating_days()
    {
        var result = ProductHistoryRules.Evaluate(ProductHistoryWindow.ThirtyDays, Now,
        [
            new(120, "CAD", Now.AddDays(-5).AddHours(-2)),
            new(90, "CAD", Now.AddDays(-5).AddHours(2)),
            Observation(1, 110)
        ], Now.AddDays(-5));

        Assert.Equal(3, result.ObservationCount);
        Assert.Equal(2, result.ObservedDayCount);
        Assert.Equal(90m, result.Points[0].LowestPrice);
        Assert.Equal(2, result.Points[0].ObservationCount);
    }

    [Fact]
    public void Reliable_thirty_day_history_has_explicit_coverage_rule()
    {
        var result = ProductHistoryRules.Evaluate(ProductHistoryWindow.ThirtyDays, Now,
            [Observation(29, 130), Observation(24, 125), Observation(19, 120), Observation(14, 115), Observation(9, 110), Observation(2, 105)], Now.AddDays(-29));

        Assert.Equal(ProductHistoryState.Reliable, result.State);
        Assert.Contains("no gap longer", result.CoverageSummary);
    }

    [Fact]
    public void Reliable_ninety_day_history_requires_longer_span_and_more_observed_days()
    {
        var result = ProductHistoryRules.Evaluate(ProductHistoryWindow.NinetyDays, Now,
            [Observation(84, 140), Observation(75, 138), Observation(66, 136), Observation(57, 134), Observation(48, 132), Observation(39, 130), Observation(30, 128), Observation(21, 126), Observation(12, 124), Observation(3, 122)], Now.AddDays(-84));

        Assert.Equal(ProductHistoryState.Reliable, result.State);
    }

    [Fact]
    public void Sparse_history_is_partial_and_never_claims_all_time_low()
    {
        var result = ProductHistoryRules.Evaluate(ProductHistoryWindow.NinetyDays, Now,
            [Observation(20, 130), Observation(2, 100)], Now.AddDays(-20));

        Assert.Equal(ProductHistoryState.Partial, result.State);
        Assert.Contains("gaps", result.CoverageSummary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("all-time", result.CoverageSummary + result.Interpretation, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Fewer_than_two_observed_days_is_unavailable(int count)
    {
        var observations = count == 0 ? Array.Empty<ProductHistoryObservation>() : [Observation(1, 100)];
        var result = ProductHistoryRules.Evaluate(ProductHistoryWindow.ThirtyDays, Now, observations, null);

        Assert.Equal(ProductHistoryState.Unavailable, result.State);
        Assert.Empty(result.Points);
        Assert.Null(result.LowestObservedPrice);
    }

    private static ProductHistoryObservation Observation(int daysAgo, decimal amount) =>
        new(amount, "CAD", Now.AddDays(-daysAgo));
}
