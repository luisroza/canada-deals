using CanadaDeals.Domain.Reporting;

namespace CanadaDeals.Api.Contracts;

public sealed record CreateListingIssueReportRequest(string? Reason, string? Note);

public sealed record CreateListingIssueReportResponse(
    Guid ReportId,
    string Status,
    string Message);

public sealed record InternalListingIssueReportResponse(
    Guid ReportId,
    Guid ListingId,
    string Retailer,
    string ListingTitle,
    string Reason,
    string? Note,
    string Status,
    DateTimeOffset CreatedAt);

public static class ListingIssueReportContractValues
{
    public static bool TryParseReason(string? value, out ListingIssueReason reason)
    {
        reason = value?.Trim().ToUpperInvariant() switch
        {
            "PRICE_CHANGED" => ListingIssueReason.PriceChanged,
            "WRONG_PRODUCT" => ListingIssueReason.WrongProduct,
            "WRONG_VARIANT" => ListingIssueReason.WrongVariant,
            "OFFER_EXPIRED" => ListingIssueReason.OfferExpired,
            "RETAILER_PAGE_UNAVAILABLE" => ListingIssueReason.RetailerPageUnavailable,
            "OTHER" => ListingIssueReason.Other,
            _ => (ListingIssueReason)(-1)
        };

        return Enum.IsDefined(reason);
    }

    public static bool TryParseStatus(string? value, out ListingIssueStatus status)
    {
        status = value?.Trim().ToUpperInvariant() switch
        {
            null or "" or "OPEN" => ListingIssueStatus.Open,
            "REVIEWED" => ListingIssueStatus.Reviewed,
            "RESOLVED" => ListingIssueStatus.Resolved,
            "DISMISSED" => ListingIssueStatus.Dismissed,
            _ => (ListingIssueStatus)(-1)
        };

        return Enum.IsDefined(status);
    }

    public static string ToContract(this ListingIssueReason reason) => reason switch
    {
        ListingIssueReason.PriceChanged => "PRICE_CHANGED",
        ListingIssueReason.WrongProduct => "WRONG_PRODUCT",
        ListingIssueReason.WrongVariant => "WRONG_VARIANT",
        ListingIssueReason.OfferExpired => "OFFER_EXPIRED",
        ListingIssueReason.RetailerPageUnavailable => "RETAILER_PAGE_UNAVAILABLE",
        ListingIssueReason.Other => "OTHER",
        _ => throw new ArgumentOutOfRangeException(nameof(reason))
    };

    public static string ToContract(this ListingIssueStatus status) => status.ToString().ToUpperInvariant();
}
