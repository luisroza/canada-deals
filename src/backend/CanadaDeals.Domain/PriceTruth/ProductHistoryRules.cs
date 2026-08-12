namespace CanadaDeals.Domain.PriceTruth;

public enum ProductHistoryWindow
{
    ThirtyDays = 30,
    NinetyDays = 90
}

public enum ProductHistoryState
{
    Reliable,
    Partial,
    Unavailable
}

public sealed record ProductHistoryObservation(decimal Amount, string Currency, DateTimeOffset ObservedAt);

public sealed record ProductHistoryPoint(
    DateTimeOffset ObservedDate,
    decimal LowestPrice,
    string Currency,
    int ObservationCount);

public sealed record ProductHistoryEvidence(
    ProductHistoryWindow Window,
    ProductHistoryState State,
    DateTimeOffset? TrackingStart,
    DateTimeOffset? ObservationStart,
    DateTimeOffset? ObservationEnd,
    decimal? LowestObservedPrice,
    decimal? HighestObservedPrice,
    int ObservationCount,
    int ObservedDayCount,
    int? LargestGapDays,
    string CoverageSummary,
    string Interpretation,
    IReadOnlyList<ProductHistoryPoint> Points);

public static class ProductHistoryRules
{
    public const string SupportedCurrency = "CAD";

    public static bool TryParseWindow(string? value, out ProductHistoryWindow window)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        window = normalized switch
        {
            null or "" or "30d" => ProductHistoryWindow.ThirtyDays,
            "90d" => ProductHistoryWindow.NinetyDays,
            _ => default
        };
        return normalized is null or "" or "30d" or "90d";
    }

    public static string WindowKey(ProductHistoryWindow window) => window switch
    {
        ProductHistoryWindow.ThirtyDays => "30d",
        ProductHistoryWindow.NinetyDays => "90d",
        _ => throw new ArgumentOutOfRangeException(nameof(window))
    };

    public static ProductHistoryEvidence Evaluate(
        ProductHistoryWindow window,
        DateTimeOffset now,
        IEnumerable<ProductHistoryObservation> observations,
        DateTimeOffset? trackingStart)
    {
        var windowStart = now.AddDays(-(int)window);
        var valid = observations
            .Where(observation =>
                observation.Amount > 0 &&
                observation.Currency.Equals(SupportedCurrency, StringComparison.OrdinalIgnoreCase) &&
                observation.ObservedAt >= windowStart &&
                observation.ObservedAt <= now)
            .OrderBy(observation => observation.ObservedAt)
            .ToList();

        var points = valid
            .GroupBy(observation => observation.ObservedAt.UtcDateTime.Date)
            .Select(group => new ProductHistoryPoint(
                new DateTimeOffset(group.Key, TimeSpan.Zero),
                group.Min(observation => observation.Amount),
                SupportedCurrency,
                group.Count()))
            .OrderBy(point => point.ObservedDate)
            .ToList();

        if (points.Count < 2)
        {
            var reason = valid.Count == 0
                ? "Price history unavailable — no qualifying permitted observations exist in this period."
                : "Price history unavailable — one qualifying observation is not enough to show a trend.";
            return new ProductHistoryEvidence(
                window,
                ProductHistoryState.Unavailable,
                trackingStart,
                null,
                null,
                null,
                null,
                valid.Count,
                points.Count,
                null,
                reason,
                "Current price and freshness remain available separately.",
                []);
        }

        var spanDays = (points[^1].ObservedDate.Date - points[0].ObservedDate.Date).Days + 1;
        var largestGapDays = points.Zip(points.Skip(1), (left, right) => (right.ObservedDate.Date - left.ObservedDate.Date).Days).Max();
        var reliable = window switch
        {
            ProductHistoryWindow.ThirtyDays => points.Count >= 6 && spanDays >= 21 && largestGapDays <= 10,
            ProductHistoryWindow.NinetyDays => points.Count >= 10 && spanDays >= 60 && largestGapDays <= 21,
            _ => false
        };
        var state = reliable ? ProductHistoryState.Reliable : ProductHistoryState.Partial;
        var coverage = reliable
            ? $"Reliable history — {points.Count} observed days span {spanDays} days, with no gap longer than {largestGapDays} days."
            : $"Partial history — {points.Count} observed days span {spanDays} days, so gaps limit stronger conclusions.";
        var interpretation = reliable
            ? "The observed evidence supports a bounded price trend for this selected period."
            : "These are real observations, but they do not imply continuous monitoring between points.";

        return new ProductHistoryEvidence(
            window,
            state,
            trackingStart,
            points[0].ObservedDate,
            points[^1].ObservedDate,
            points.Min(point => point.LowestPrice),
            points.Max(point => point.LowestPrice),
            valid.Count,
            points.Count,
            largestGapDays,
            coverage,
            interpretation,
            points);
    }
}
