using CanadaDeals.Domain.Common;

namespace CanadaDeals.Domain.Alerts;

public enum PriceAlertEvaluationOutcome
{
    Eligible = 0,
    AboveTarget = 1,
    SkippedNoPublishablePrice = 2,
    SkippedPolicy = 3,
    SkippedUnsafeMatch = 4,
    SkippedUnavailable = 5,
    SkippedStale = 6,
    AlreadyTriggeredForCondition = 7
}

public sealed record AlertPriceCandidate(
    Guid PriceObservationId,
    decimal Amount,
    string Currency,
    DateTimeOffset ObservedAt,
    bool IsObservationPermitted,
    PolicyPermission PricePolicy,
    MatchState MatchState,
    OnlineAvailabilityState Availability,
    int? MaximumAgeHours);

public sealed record PriceAlertEvaluationResult(
    PriceAlertEvaluationOutcome Outcome,
    AlertPriceCandidate? QualifyingCandidate);

public static class PriceAlertEvaluator
{
    public static PriceAlertEvaluationResult Evaluate(
        PriceAlert alert,
        IReadOnlyCollection<AlertPriceCandidate> candidates,
        DateTimeOffset now)
    {
        if (alert.Status != PriceAlertStatus.Active || candidates.Count == 0)
            return new(PriceAlertEvaluationOutcome.SkippedNoPublishablePrice, null);

        var policyPermitted = candidates
            .Where(x => x.IsObservationPermitted && x.PricePolicy == PolicyPermission.Allowed)
            .ToList();
        if (policyPermitted.Count == 0) return new(PriceAlertEvaluationOutcome.SkippedPolicy, null);

        var safelyMatched = policyPermitted
            .Where(x => x.MatchState is MatchState.AutoMatched or MatchState.Confirmed)
            .ToList();
        if (safelyMatched.Count == 0) return new(PriceAlertEvaluationOutcome.SkippedUnsafeMatch, null);

        var available = safelyMatched
            .Where(x => x.Availability == OnlineAvailabilityState.Available)
            .ToList();
        if (available.Count == 0) return new(PriceAlertEvaluationOutcome.SkippedUnavailable, null);

        var validPrices = available
            .Where(x => x.Amount > 0 && string.Equals(x.Currency, alert.Currency, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (validPrices.Count == 0) return new(PriceAlertEvaluationOutcome.SkippedNoPublishablePrice, null);

        var fresh = validPrices
            .Where(x => x.ObservedAt <= now && now - x.ObservedAt <= TimeSpan.FromHours(x.MaximumAgeHours ?? 24))
            .OrderBy(x => x.Amount)
            .ThenByDescending(x => x.ObservedAt)
            .ToList();
        if (fresh.Count == 0) return new(PriceAlertEvaluationOutcome.SkippedStale, null);

        var qualifying = fresh[0];
        if (qualifying.Amount > alert.TargetPrice)
            return new(PriceAlertEvaluationOutcome.AboveTarget, qualifying);
        if (alert.IsBelowTargetCycle)
            return new(PriceAlertEvaluationOutcome.AlreadyTriggeredForCondition, qualifying);

        return new(PriceAlertEvaluationOutcome.Eligible, qualifying);
    }
}
