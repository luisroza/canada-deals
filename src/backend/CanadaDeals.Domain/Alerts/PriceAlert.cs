using CanadaDeals.Domain.Catalog;

namespace CanadaDeals.Domain.Alerts;

public enum PriceAlertStatus
{
    Active = 0,
    Disabled = 1
}

public enum NotificationDeliveryStatus
{
    Pending = 0,
    DevelopmentCaptured = 1,
    ProviderAccepted = 2,
    TransientFailure = 3,
    Suppressed = 4,
    Delivered = 5,
    PermanentFailure = 6,
    Bounced = 7,
    Complained = 8
}

public sealed class PriceAlert
{
    public const decimal MaximumTargetPrice = 1_000_000m;
    public const string SupportedCurrency = "CAD";
    public const string CurrentConsentVersion = "target-price-email-v1";

    private PriceAlert() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public decimal TargetPrice { get; private set; }
    public string Currency { get; private set; } = SupportedCurrency;
    public PriceAlertStatus Status { get; private set; }
    public int TargetVersion { get; private set; }
    public bool IsBelowTargetCycle { get; private set; }
    public DateTimeOffset ConsentGrantedAt { get; private set; }
    public string ConsentVersion { get; private set; } = CurrentConsentVersion;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? LastEvaluatedAt { get; private set; }
    public DateTimeOffset? LastTriggeredAt { get; private set; }

    public static PriceAlert Create(
        Guid userId,
        Guid productId,
        decimal targetPrice,
        string currency,
        DateTimeOffset consentGrantedAt,
        string consentVersion,
        DateTimeOffset createdAt)
    {
        if (userId == Guid.Empty) throw new ArgumentException("A price alert requires a user.", nameof(userId));
        if (productId == Guid.Empty) throw new ArgumentException("A price alert requires a product.", nameof(productId));
        ValidateTarget(targetPrice, currency);
        if (string.IsNullOrWhiteSpace(consentVersion)) throw new ArgumentException("Consent version is required.", nameof(consentVersion));

        return new PriceAlert
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProductId = productId,
            TargetPrice = targetPrice,
            Currency = currency.ToUpperInvariant(),
            Status = PriceAlertStatus.Active,
            TargetVersion = 1,
            ConsentGrantedAt = consentGrantedAt,
            ConsentVersion = consentVersion.Trim(),
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public bool SetTarget(decimal targetPrice, string currency, DateTimeOffset consentGrantedAt, string consentVersion, DateTimeOffset updatedAt)
    {
        ValidateTarget(targetPrice, currency);
        if (string.IsNullOrWhiteSpace(consentVersion)) throw new ArgumentException("Consent version is required.", nameof(consentVersion));

        var normalizedCurrency = currency.ToUpperInvariant();
        var changed = TargetPrice != targetPrice || Currency != normalizedCurrency || Status != PriceAlertStatus.Active;
        ConsentGrantedAt = consentGrantedAt;
        ConsentVersion = consentVersion.Trim();
        UpdatedAt = updatedAt;
        Status = PriceAlertStatus.Active;

        if (!changed) return false;

        TargetPrice = targetPrice;
        Currency = normalizedCurrency;
        TargetVersion++;
        IsBelowTargetCycle = false;
        return true;
    }

    public void Disable(DateTimeOffset disabledAt)
    {
        Status = PriceAlertStatus.Disabled;
        UpdatedAt = disabledAt;
        IsBelowTargetCycle = false;
    }

    public void RecordEvaluation(DateTimeOffset evaluatedAt) => LastEvaluatedAt = evaluatedAt;

    public void RecordAboveTarget(DateTimeOffset evaluatedAt)
    {
        LastEvaluatedAt = evaluatedAt;
        IsBelowTargetCycle = false;
    }

    public void RecordTriggered(DateTimeOffset triggeredAt)
    {
        LastEvaluatedAt = triggeredAt;
        LastTriggeredAt = triggeredAt;
        IsBelowTargetCycle = true;
    }

    private static void ValidateTarget(decimal targetPrice, string currency)
    {
        if (targetPrice <= 0 || targetPrice > MaximumTargetPrice)
            throw new ArgumentOutOfRangeException(nameof(targetPrice), $"Target price must be between 0.01 and {MaximumTargetPrice:0.00}.");
        if (decimal.Round(targetPrice, 2) != targetPrice)
            throw new ArgumentException("Target price supports at most two decimal places.", nameof(targetPrice));
        if (!string.Equals(currency, SupportedCurrency, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only CAD target prices are supported in the MVP.", nameof(currency));
    }
}

public sealed class NotificationDelivery
{
    private NotificationDelivery() { }

    public Guid Id { get; private set; }
    public Guid PriceAlertId { get; private set; }
    public PriceAlert PriceAlert { get; private set; } = null!;
    public Guid PriceObservationId { get; private set; }
    public int TargetVersion { get; private set; }
    public decimal TargetPrice { get; private set; }
    public decimal QualifyingPrice { get; private set; }
    public string Currency { get; private set; } = PriceAlert.SupportedCurrency;
    public string Channel { get; private set; } = "EMAIL";
    public string DestinationAddress { get; private set; } = string.Empty;
    public NotificationDeliveryStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public string? StatusReason { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastAttemptedAt { get; private set; }
    public DateTimeOffset? NextAttemptAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public DateTimeOffset? ProviderAcceptedAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public DateTimeOffset? LastProviderEventAt { get; private set; }

    public string IdempotencyKey => $"price-alert/{Id:N}";

    public static NotificationDelivery Create(
        Guid alertId,
        Guid observationId,
        int targetVersion,
        decimal targetPrice,
        decimal qualifyingPrice,
        string currency,
        string destinationAddress,
        DateTimeOffset createdAt)
    {
        if (alertId == Guid.Empty || observationId == Guid.Empty) throw new ArgumentException("Alert and observation are required.");
        if (string.IsNullOrWhiteSpace(destinationAddress)) throw new ArgumentException("A delivery destination is required.", nameof(destinationAddress));

        return new NotificationDelivery
        {
            Id = Guid.NewGuid(),
            PriceAlertId = alertId,
            PriceObservationId = observationId,
            TargetVersion = targetVersion,
            TargetPrice = targetPrice,
            QualifyingPrice = qualifyingPrice,
            Currency = currency.ToUpperInvariant(),
            DestinationAddress = destinationAddress.Trim(),
            Status = NotificationDeliveryStatus.Pending,
            CreatedAt = createdAt
        };
    }

    public void CaptureForDevelopment(DateTimeOffset capturedAt)
    {
        Status = NotificationDeliveryStatus.DevelopmentCaptured;
        StatusReason = "CONTROLLED_DEVELOPMENT_TEST_CAPTURE";
        ProcessedAt = capturedAt;
        NextAttemptAt = null;
    }

    public void RecordAttempt(DateTimeOffset attemptedAt)
    {
        AttemptCount++;
        LastAttemptedAt = attemptedAt;
        if (Status == NotificationDeliveryStatus.TransientFailure) Status = NotificationDeliveryStatus.Pending;
        NextAttemptAt = null;
    }

    public void Accept(string providerMessageId, DateTimeOffset acceptedAt)
    {
        ProviderMessageId = string.IsNullOrWhiteSpace(providerMessageId) ? throw new ArgumentException("Provider message ID is required.", nameof(providerMessageId)) : providerMessageId;
        Status = NotificationDeliveryStatus.ProviderAccepted;
        StatusReason = null;
        ProcessedAt = acceptedAt;
        ProviderAcceptedAt = acceptedAt;
        NextAttemptAt = null;
    }

    public void FailTransient(string reason, DateTimeOffset nextAttemptAt)
    {
        Status = NotificationDeliveryStatus.TransientFailure;
        StatusReason = reason;
        NextAttemptAt = nextAttemptAt;
    }

    public void FailPermanent(string reason, DateTimeOffset failedAt)
    {
        Status = NotificationDeliveryStatus.PermanentFailure;
        StatusReason = reason;
        ProcessedAt = failedAt;
        NextAttemptAt = null;
    }

    public bool ApplyProviderEvent(string eventType, DateTimeOffset eventAt)
    {
        if (LastProviderEventAt.HasValue && eventAt < LastProviderEventAt.Value) return false;
        var next = eventType switch
        {
            "email.delivered" => NotificationDeliveryStatus.Delivered,
            "email.bounced" => NotificationDeliveryStatus.Bounced,
            "email.complained" => NotificationDeliveryStatus.Complained,
            "email.suppressed" => NotificationDeliveryStatus.Suppressed,
            "email.failed" => NotificationDeliveryStatus.PermanentFailure,
            "email.sent" => NotificationDeliveryStatus.ProviderAccepted,
            _ => Status
        };
        if (next == NotificationDeliveryStatus.ProviderAccepted && Status is NotificationDeliveryStatus.Delivered or NotificationDeliveryStatus.Bounced or NotificationDeliveryStatus.Complained or NotificationDeliveryStatus.Suppressed or NotificationDeliveryStatus.PermanentFailure) return false;
        Status = next;
        LastProviderEventAt = eventAt;
        ProcessedAt = eventAt;
        if (next == NotificationDeliveryStatus.Delivered) DeliveredAt = eventAt;
        StatusReason = next is NotificationDeliveryStatus.Bounced or NotificationDeliveryStatus.Complained or NotificationDeliveryStatus.Suppressed or NotificationDeliveryStatus.PermanentFailure ? eventType.ToUpperInvariant().Replace('.', '_') : null;
        return true;
    }

    public void Suppress(string reason, DateTimeOffset suppressedAt)
    {
        Status = NotificationDeliveryStatus.Suppressed;
        StatusReason = reason;
        ProcessedAt = suppressedAt;
        NextAttemptAt = null;
    }
}
