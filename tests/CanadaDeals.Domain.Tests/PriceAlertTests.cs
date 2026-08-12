using CanadaDeals.Domain.Alerts;
using CanadaDeals.Domain.Common;

namespace CanadaDeals.Domain.Tests;

public sealed class PriceAlertTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-08-11T20:00:00Z");

    [Fact]
    public void Create_records_active_CAD_target_version_and_explicit_consent()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var alert = PriceAlert.Create(userId, productId, 100m, "CAD", Now, PriceAlert.CurrentConsentVersion, Now);

        Assert.Equal(userId, alert.UserId);
        Assert.Equal(productId, alert.ProductId);
        Assert.Equal(100m, alert.TargetPrice);
        Assert.Equal("CAD", alert.Currency);
        Assert.Equal(PriceAlertStatus.Active, alert.Status);
        Assert.Equal(1, alert.TargetVersion);
        Assert.Equal(PriceAlert.CurrentConsentVersion, alert.ConsentVersion);
        Assert.Equal(Now, alert.ConsentGrantedAt);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1000000.01")]
    public void Create_rejects_targets_outside_the_approved_range(string value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateAlert(decimal.Parse(value)));
    }

    [Fact]
    public void Create_rejects_more_than_two_decimal_places_and_non_CAD_currency()
    {
        Assert.Throws<ArgumentException>(() => CreateAlert(1.001m));
        Assert.Throws<ArgumentException>(() => PriceAlert.Create(Guid.NewGuid(), Guid.NewGuid(), 10m, "USD", Now, PriceAlert.CurrentConsentVersion, Now));
    }

    [Fact]
    public void Setting_the_same_active_target_is_idempotent_but_refreshes_consent()
    {
        var alert = CreateAlert(100m);

        var changed = alert.SetTarget(100m, "cad", Now.AddMinutes(1), PriceAlert.CurrentConsentVersion, Now.AddMinutes(1));

        Assert.False(changed);
        Assert.Equal(1, alert.TargetVersion);
        Assert.Equal(Now.AddMinutes(1), alert.ConsentGrantedAt);
    }

    [Fact]
    public void Changing_or_reactivating_a_target_increments_version_and_resets_the_below_target_cycle()
    {
        var alert = CreateAlert(100m);
        alert.RecordTriggered(Now.AddMinutes(1));

        Assert.True(alert.SetTarget(90m, "CAD", Now.AddMinutes(2), PriceAlert.CurrentConsentVersion, Now.AddMinutes(2)));
        Assert.Equal(2, alert.TargetVersion);
        Assert.False(alert.IsBelowTargetCycle);
        alert.Disable(Now.AddMinutes(3));
        Assert.True(alert.SetTarget(90m, "CAD", Now.AddMinutes(4), PriceAlert.CurrentConsentVersion, Now.AddMinutes(4)));
        Assert.Equal(3, alert.TargetVersion);
        Assert.Equal(PriceAlertStatus.Active, alert.Status);
    }

    [Fact]
    public void Equality_is_a_qualifying_price()
    {
        var result = PriceAlertEvaluator.Evaluate(CreateAlert(100m), [Candidate(100m)], Now);

        Assert.Equal(PriceAlertEvaluationOutcome.Eligible, result.Outcome);
    }

    [Fact]
    public void Above_target_resets_the_cycle_and_allows_a_later_drop_to_trigger_again()
    {
        var alert = CreateAlert(100m);
        alert.RecordTriggered(Now.AddMinutes(-2));
        var above = PriceAlertEvaluator.Evaluate(alert, [Candidate(101m)], Now);
        Assert.Equal(PriceAlertEvaluationOutcome.AboveTarget, above.Outcome);

        alert.RecordAboveTarget(Now);
        var drop = PriceAlertEvaluator.Evaluate(alert, [Candidate(99m, observedAt: Now.AddMinutes(1))], Now.AddMinutes(1));
        Assert.Equal(PriceAlertEvaluationOutcome.Eligible, drop.Outcome);
    }

    [Fact]
    public void Continuous_below_target_condition_is_not_retriggered()
    {
        var alert = CreateAlert(100m);
        alert.RecordTriggered(Now.AddMinutes(-1));

        var result = PriceAlertEvaluator.Evaluate(alert, [Candidate(90m)], Now);

        Assert.Equal(PriceAlertEvaluationOutcome.AlreadyTriggeredForCondition, result.Outcome);
    }

    [Fact]
    public void Stale_price_is_not_eligible()
    {
        var result = PriceAlertEvaluator.Evaluate(CreateAlert(100m), [Candidate(90m, observedAt: Now.AddHours(-25))], Now);

        Assert.Equal(PriceAlertEvaluationOutcome.SkippedStale, result.Outcome);
    }

    [Fact]
    public void Future_dated_observation_is_not_eligible()
    {
        var result = PriceAlertEvaluator.Evaluate(CreateAlert(100m), [Candidate(90m, observedAt: Now.AddMinutes(1))], Now);

        Assert.Equal(PriceAlertEvaluationOutcome.SkippedStale, result.Outcome);
    }

    [Fact]
    public void Policy_denied_observation_is_not_eligible()
    {
        var result = PriceAlertEvaluator.Evaluate(CreateAlert(100m), [Candidate(90m, policy: PolicyPermission.Denied)], Now);

        Assert.Equal(PriceAlertEvaluationOutcome.SkippedPolicy, result.Outcome);
    }

    [Fact]
    public void Possible_match_review_is_not_eligible_even_when_cheaper()
    {
        var result = PriceAlertEvaluator.Evaluate(CreateAlert(100m), [Candidate(80m, matchState: MatchState.PossibleMatchReview)], Now);

        Assert.Equal(PriceAlertEvaluationOutcome.SkippedUnsafeMatch, result.Outcome);
    }

    [Fact]
    public void Unavailable_offer_is_not_eligible()
    {
        var result = PriceAlertEvaluator.Evaluate(CreateAlert(100m), [Candidate(80m, availability: OnlineAvailabilityState.Unavailable)], Now);

        Assert.Equal(PriceAlertEvaluationOutcome.SkippedUnavailable, result.Outcome);
    }

    [Fact]
    public void History_commission_and_save_popularity_are_not_evaluation_inputs()
    {
        var inputs = typeof(AlertPriceCandidate).GetProperties().Select(x => x.Name).ToArray();

        Assert.DoesNotContain(inputs, x => x.Contains("History", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(inputs, x => x.Contains("Commission", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(inputs, x => x.Contains("Save", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(PriceAlertEvaluationOutcome.Eligible, PriceAlertEvaluator.Evaluate(CreateAlert(100m), [Candidate(90m)], Now).Outcome);
    }

    private static PriceAlert CreateAlert(decimal target) =>
        PriceAlert.Create(Guid.NewGuid(), Guid.NewGuid(), target, "CAD", Now, PriceAlert.CurrentConsentVersion, Now);

    private static AlertPriceCandidate Candidate(
        decimal amount,
        DateTimeOffset? observedAt = null,
        PolicyPermission policy = PolicyPermission.Allowed,
        MatchState matchState = MatchState.Confirmed,
        OnlineAvailabilityState availability = OnlineAvailabilityState.Available) =>
        new(Guid.NewGuid(), amount, "CAD", observedAt ?? Now.AddMinutes(-5), true, policy, matchState, availability, 24);
}
