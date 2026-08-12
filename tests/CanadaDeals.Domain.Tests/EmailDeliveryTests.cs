using CanadaDeals.Domain.Alerts;
using CanadaDeals.Domain.Notifications;

namespace CanadaDeals.Domain.Tests;

public sealed class EmailDeliveryTests
{
    [Fact]
    public void Alert_delivery_has_stable_identity_derived_idempotency_key()
    {
        var delivery = CreateAlertDelivery();
        Assert.Equal($"price-alert/{delivery.Id:N}", delivery.IdempotencyKey);
        Assert.Equal(delivery.IdempotencyKey, delivery.IdempotencyKey);
    }

    [Fact]
    public void Provider_acceptance_is_not_recorded_as_delivery()
    {
        var now = DateTimeOffset.UtcNow;
        var delivery = CreateAlertDelivery();
        delivery.RecordAttempt(now);
        delivery.Accept("provider-1", now);
        Assert.Equal(NotificationDeliveryStatus.ProviderAccepted, delivery.Status);
        Assert.Null(delivery.DeliveredAt);
        Assert.Equal("provider-1", delivery.ProviderMessageId);
    }

    [Fact]
    public void Older_sent_event_cannot_regress_delivered_state()
    {
        var now = DateTimeOffset.UtcNow;
        var delivery = CreateAlertDelivery();
        delivery.Accept("provider-1", now.AddMinutes(-2));
        Assert.True(delivery.ApplyProviderEvent("email.delivered", now));
        Assert.False(delivery.ApplyProviderEvent("email.sent", now.AddMinutes(-1)));
        Assert.Equal(NotificationDeliveryStatus.Delivered, delivery.Status);
    }

    [Fact]
    public void Later_bounce_can_suppress_a_previously_delivered_address()
    {
        var now = DateTimeOffset.UtcNow;
        var delivery = CreateAlertDelivery();
        delivery.Accept("provider-1", now.AddMinutes(-2));
        delivery.ApplyProviderEvent("email.delivered", now.AddMinutes(-1));
        Assert.True(delivery.ApplyProviderEvent("email.bounced", now));
        Assert.Equal(NotificationDeliveryStatus.Bounced, delivery.Status);
    }

    [Fact]
    public void Transient_failure_remains_retryable_and_counts_durable_attempts()
    {
        var delivery = CreateAlertDelivery();
        delivery.RecordAttempt(DateTimeOffset.UtcNow);
        var retryAt = DateTimeOffset.UtcNow.AddMinutes(3);
        delivery.FailTransient("PROVIDER_HTTP_429", retryAt);
        Assert.Equal(NotificationDeliveryStatus.TransientFailure, delivery.Status);
        Assert.Equal(1, delivery.AttemptCount);
        Assert.Equal("PROVIDER_HTTP_429", delivery.StatusReason);
        Assert.Equal(retryAt, delivery.NextAttemptAt);
    }

    [Fact]
    public void Account_confirmation_delivery_has_separate_stable_identity()
    {
        var delivery = AccountConfirmationDelivery.Create(Guid.NewGuid(), "person@example.test", DateTimeOffset.UtcNow);
        Assert.Equal($"account-confirmation/{delivery.Id:N}", delivery.IdempotencyKey);
        Assert.Equal(EmailDeliveryStatus.Pending, delivery.Status);
    }

    private static NotificationDelivery CreateAlertDelivery() => NotificationDelivery.Create(
        Guid.NewGuid(), Guid.NewGuid(), 1, 100m, 90m, "CAD", "person@example.test", DateTimeOffset.UtcNow);
}
