using CanadaDeals.Domain.Retailers;

namespace CanadaDeals.Domain.Reporting;

public enum ListingIssueReason
{
    PriceChanged = 0,
    WrongProduct = 1,
    WrongVariant = 2,
    OfferExpired = 3,
    RetailerPageUnavailable = 4,
    Other = 5
}

public enum ListingIssueStatus
{
    Open = 0,
    Reviewed = 1,
    Resolved = 2,
    Dismissed = 3
}

public sealed class ListingIssueReport
{
    public const int MaxNoteLength = 500;

    private ListingIssueReport() { }

    public Guid Id { get; private set; }
    public Guid RetailerListingId { get; private set; }
    public RetailerListing RetailerListing { get; private set; } = null!;
    public ListingIssueReason Reason { get; private set; }
    public string? Note { get; private set; }
    public ListingIssueStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static ListingIssueReport Create(
        Guid retailerListingId,
        ListingIssueReason reason,
        string? note,
        DateTimeOffset createdAt)
    {
        if (retailerListingId == Guid.Empty)
            throw new ArgumentException("Retailer listing ID is required.", nameof(retailerListingId));
        if (!Enum.IsDefined(reason))
            throw new ArgumentOutOfRangeException(nameof(reason), "A supported report reason is required.");

        var normalizedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (normalizedNote?.Length > MaxNoteLength)
            throw new ArgumentException($"The note cannot exceed {MaxNoteLength} characters.", nameof(note));

        return new ListingIssueReport
        {
            Id = Guid.NewGuid(),
            RetailerListingId = retailerListingId,
            Reason = reason,
            Note = normalizedNote,
            Status = ListingIssueStatus.Open,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public void ChangeStatus(ListingIssueStatus status, DateTimeOffset updatedAt)
    {
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        if (updatedAt < CreatedAt) throw new ArgumentOutOfRangeException(nameof(updatedAt));
        Status = status;
        UpdatedAt = updatedAt;
    }
}
