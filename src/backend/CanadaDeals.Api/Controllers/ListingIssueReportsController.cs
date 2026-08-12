using CanadaDeals.Api.Contracts;
using CanadaDeals.Domain.Reporting;
using CanadaDeals.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanadaDeals.Api.Controllers;

[ApiController]
[Route("api/v1/listings/{listingId:guid}/reports")]
public sealed class ListingIssueReportsController(
    DealsDbContext db,
    TimeProvider clock,
    ILogger<ListingIssueReportsController> logger) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CreateListingIssueReportResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        Guid listingId,
        CreateListingIssueReportRequest request,
        CancellationToken cancellationToken)
    {
        if (!ListingIssueReportContractValues.TryParseReason(request.Reason, out var reason))
        {
            ModelState.AddModelError(nameof(request.Reason), "Choose a supported report reason.");
            return ValidationProblem(ModelState);
        }

        var normalizedNote = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        if (normalizedNote?.Length > ListingIssueReport.MaxNoteLength)
        {
            ModelState.AddModelError(nameof(request.Note), $"The note cannot exceed {ListingIssueReport.MaxNoteLength} characters.");
            return ValidationProblem(ModelState);
        }

        if (!await db.RetailerListings.AnyAsync(x => x.Id == listingId, cancellationToken))
            return NotFound();

        var report = ListingIssueReport.Create(listingId, reason, normalizedNote, clock.GetUtcNow());
        db.ListingIssueReports.Add(report);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Listing issue report {ReportId} created for listing {ListingId} with reason {Reason}.",
            report.Id,
            listingId,
            reason.ToContract());

        return StatusCode(StatusCodes.Status201Created, new CreateListingIssueReportResponse(
            report.Id,
            report.Status.ToContract(),
            "Thanks. Your report was attached to this listing for review."));
    }
}
