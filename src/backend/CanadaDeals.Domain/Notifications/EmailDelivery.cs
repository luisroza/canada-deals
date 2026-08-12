namespace CanadaDeals.Domain.Notifications;

public enum EmailDeliveryStatus
{
    Pending = 0,
    DevelopmentCaptured = 1,
    ProviderAccepted = 2,
    TransientFailure = 3,
    PermanentFailure = 4,
    Suppressed = 5,
    Delivered = 6,
    Bounced = 7,
    Complained = 8
}

public sealed class AccountConfirmationDelivery
{
    private AccountConfirmationDelivery() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string DestinationAddress { get; private set; } = string.Empty;
    public EmailDeliveryStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public string? StatusReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastAttemptedAt { get; private set; }
    public DateTimeOffset? ProviderAcceptedAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public DateTimeOffset? LastProviderEventAt { get; private set; }

    public string IdempotencyKey => $"account-confirmation/{Id:N}";

    public static AccountConfirmationDelivery Create(Guid userId, string destinationAddress, DateTimeOffset createdAt)
    {
        if (userId == Guid.Empty) throw new ArgumentException("A user is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(destinationAddress)) throw new ArgumentException("A destination is required.", nameof(destinationAddress));
        return new AccountConfirmationDelivery
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DestinationAddress = destinationAddress.Trim(),
            Status = EmailDeliveryStatus.Pending,
            CreatedAt = createdAt
        };
    }

    public void RecordAttempt(DateTimeOffset attemptedAt)
    {
        AttemptCount++;
        LastAttemptedAt = attemptedAt;
    }

    public void CaptureForDevelopment(DateTimeOffset at) => SetTerminal(EmailDeliveryStatus.DevelopmentCaptured, "CONTROLLED_DEVELOPMENT_TEST_CAPTURE", at);
    public void Accept(string providerMessageId, DateTimeOffset at)
    {
        ProviderMessageId = string.IsNullOrWhiteSpace(providerMessageId) ? throw new ArgumentException("Provider message ID is required.", nameof(providerMessageId)) : providerMessageId;
        Status = EmailDeliveryStatus.ProviderAccepted;
        StatusReason = null;
        ProviderAcceptedAt = at;
    }
    public void FailTransient(string reason) { Status = EmailDeliveryStatus.TransientFailure; StatusReason = reason; }
    public void FailPermanent(string reason, DateTimeOffset at) => SetTerminal(EmailDeliveryStatus.PermanentFailure, reason, at);
    public void Suppress(string reason, DateTimeOffset at) => SetTerminal(EmailDeliveryStatus.Suppressed, reason, at);

    public bool ApplyProviderEvent(string eventType, DateTimeOffset eventAt)
    {
        if (LastProviderEventAt.HasValue && eventAt < LastProviderEventAt.Value) return false;
        var next = eventType switch
        {
            "email.delivered" => EmailDeliveryStatus.Delivered,
            "email.bounced" => EmailDeliveryStatus.Bounced,
            "email.complained" => EmailDeliveryStatus.Complained,
            "email.suppressed" => EmailDeliveryStatus.Suppressed,
            "email.failed" => EmailDeliveryStatus.PermanentFailure,
            "email.sent" => EmailDeliveryStatus.ProviderAccepted,
            _ => Status
        };
        if (next == EmailDeliveryStatus.ProviderAccepted && Status is EmailDeliveryStatus.Delivered or EmailDeliveryStatus.Bounced or EmailDeliveryStatus.Complained or EmailDeliveryStatus.Suppressed or EmailDeliveryStatus.PermanentFailure) return false;
        Status = next;
        LastProviderEventAt = eventAt;
        if (next == EmailDeliveryStatus.Delivered) DeliveredAt = eventAt;
        StatusReason = next is EmailDeliveryStatus.Bounced or EmailDeliveryStatus.Complained or EmailDeliveryStatus.Suppressed or EmailDeliveryStatus.PermanentFailure ? eventType.ToUpperInvariant().Replace('.', '_') : null;
        return true;
    }

    private void SetTerminal(EmailDeliveryStatus status, string reason, DateTimeOffset at)
    {
        Status = status;
        StatusReason = reason;
        LastAttemptedAt ??= at;
    }
}

public sealed class ControlledEmailCapture
{
    private ControlledEmailCapture() { }
    public Guid Id { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string DestinationAddress { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string HtmlBody { get; private set; } = string.Empty;
    public string TextBody { get; private set; } = string.Empty;
    public DateTimeOffset CapturedAt { get; private set; }

    public static ControlledEmailCapture Create(string key, string destination, string subject, string html, string text, DateTimeOffset at) => new()
    {
        Id = Guid.NewGuid(), IdempotencyKey = key, DestinationAddress = destination, Subject = subject, HtmlBody = html, TextBody = text, CapturedAt = at
    };
}

public sealed class ProcessedEmailWebhook
{
    private ProcessedEmailWebhook() { }
    public Guid Id { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string EventId { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public string? ProviderMessageId { get; private set; }
    public DateTimeOffset ProviderCreatedAt { get; private set; }
    public DateTimeOffset ProcessedAt { get; private set; }
    public static ProcessedEmailWebhook Create(string provider, string eventId, string eventType, string? messageId, DateTimeOffset createdAt, DateTimeOffset processedAt) => new()
    {
        Id = Guid.NewGuid(), Provider = provider, EventId = eventId, EventType = eventType, ProviderMessageId = messageId, ProviderCreatedAt = createdAt, ProcessedAt = processedAt
    };
}

public sealed class EmailSuppression
{
    private EmailSuppression() { }
    public Guid Id { get; private set; }
    public string NormalizedAddress { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public static EmailSuppression Create(string address, string reason, DateTimeOffset at) => new()
    {
        Id = Guid.NewGuid(), NormalizedAddress = address.Trim().ToUpperInvariant(), Reason = reason, CreatedAt = at, UpdatedAt = at
    };
    public void Update(string reason, DateTimeOffset at) { Reason = reason; UpdatedAt = at; }
}
