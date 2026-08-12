using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.Reporting;
using CanadaDeals.Domain.Retailers;

namespace CanadaDeals.Domain.Tests;

public sealed class ListingIssueReportTests
{
    [Fact]
    public void New_report_starts_open_and_normalizes_note()
    {
        var createdAt = new DateTimeOffset(2026, 8, 11, 15, 0, 0, TimeSpan.Zero);

        var report = ListingIssueReport.Create(Guid.NewGuid(), ListingIssueReason.PriceChanged, "  Retailer shows a different price.  ", createdAt);

        Assert.Equal(ListingIssueStatus.Open, report.Status);
        Assert.Equal("Retailer shows a different price.", report.Note);
        Assert.Equal(createdAt, report.CreatedAt);
        Assert.Equal(createdAt, report.UpdatedAt);
    }

    [Theory]
    [InlineData(ListingIssueReason.PriceChanged)]
    [InlineData(ListingIssueReason.WrongProduct)]
    [InlineData(ListingIssueReason.WrongVariant)]
    [InlineData(ListingIssueReason.OfferExpired)]
    [InlineData(ListingIssueReason.RetailerPageUnavailable)]
    [InlineData(ListingIssueReason.Other)]
    public void Supported_reasons_are_accepted(ListingIssueReason reason)
    {
        var report = ListingIssueReport.Create(Guid.NewGuid(), reason, null, DateTimeOffset.UtcNow);

        Assert.Equal(reason, report.Reason);
    }

    [Fact]
    public void Whitespace_note_is_normalized_to_null()
    {
        var report = ListingIssueReport.Create(Guid.NewGuid(), ListingIssueReason.Other, "   ", DateTimeOffset.UtcNow);

        Assert.Null(report.Note);
    }

    [Fact]
    public void Excessive_note_is_rejected()
    {
        var note = new string('x', ListingIssueReport.MaxNoteLength + 1);

        Assert.Throws<ArgumentException>(() =>
            ListingIssueReport.Create(Guid.NewGuid(), ListingIssueReason.Other, note, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Unsupported_reason_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ListingIssueReport.Create(Guid.NewGuid(), (ListingIssueReason)999, null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Creating_report_does_not_mutate_listing_truth()
    {
        var listing = RetailerListing.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "fixture-listing",
            "Fixture product",
            "https://demo.local/product",
            Guid.NewGuid(),
            MatchState.Confirmed,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            549m,
            "CAD",
            FreshnessState.Recent,
            EvidenceState.Strong,
            HistoryAvailability.Reliable);
        var originalPrice = listing.CurrentPriceAmount;
        var originalMatch = listing.MatchState;

        _ = ListingIssueReport.Create(listing.Id, ListingIssueReason.PriceChanged, null, DateTimeOffset.UtcNow);

        Assert.Equal(originalPrice, listing.CurrentPriceAmount);
        Assert.Equal(originalMatch, listing.MatchState);
    }
}
