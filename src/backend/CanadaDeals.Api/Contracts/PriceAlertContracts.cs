using System.ComponentModel.DataAnnotations;

namespace CanadaDeals.Api.Contracts;

public sealed record ControlledPriceObservationRequest(
    [Range(typeof(decimal), "0.01", "1000000")] decimal Price,
    string ListingScope = "safe");

public sealed record ControlledPriceObservationResponse(
    Guid ProductId,
    Guid ListingId,
    Guid ObservationId,
    decimal Price,
    string Currency,
    string ListingScope);

public sealed record AlertEvaluationJobResponse(string JobId);
public sealed record ControlledJobStatusResponse(string JobId, string State);

public sealed record ControlledNotificationDeliveryResponse(
    Guid DeliveryId,
    Guid AlertId,
    Guid ProductId,
    decimal TargetPrice,
    decimal QualifyingPrice,
    string Currency,
    string Status,
    string? StatusReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ProcessedAt);
