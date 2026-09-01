using System.ComponentModel.DataAnnotations;

namespace CanadaDeals.Api.Contracts;

public sealed record RegisterRequest(
    [Required, EmailAddress, MaxLength(254)] string Email,
    [Required, MinLength(10), MaxLength(128)] string Password);

public sealed record LoginRequest(
    [Required, EmailAddress, MaxLength(254)] string Email,
    [Required, MaxLength(128)] string Password);
public sealed record ConfirmEmailRequest(Guid UserId, [Required, MaxLength(4096)] string Code);
public sealed record ResendConfirmationRequest([Required, EmailAddress, MaxLength(254)] string Email);
public sealed record EmailConfirmationResponse(string Status, string Message);
public sealed record AccountSessionResponse(bool IsAuthenticated, string? Email, bool EmailConfirmed);
public sealed record AccountMutationResponse(string Message, bool IsAuthenticated);
public sealed record AntiforgeryTokenResponse(string RequestToken);
public sealed record SavedOfferMutationResponse(Guid ListingId, bool IsSaved);

public sealed record UpsertPriceAlertRequest(
    [Range(typeof(decimal), "0.01", "1000000")] decimal TargetPrice,
    bool ConsentToEmail);

public sealed record PriceAlertResponse(
    Guid ProductId,
    string ProductSlug,
    string ProductTitle,
    decimal TargetPrice,
    string Currency,
    string Status,
    int TargetVersion,
    DateTimeOffset ConsentGrantedAt,
    string ConsentVersion,
    DateTimeOffset? LastEvaluatedAt,
    DateTimeOffset? LastTriggeredAt);

public sealed record PriceAlertMutationResponse(
    Guid ProductId,
    decimal TargetPrice,
    string Currency,
    string Status,
    int TargetVersion,
    string Message);
