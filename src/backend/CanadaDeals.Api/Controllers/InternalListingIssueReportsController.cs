using CanadaDeals.Api.Contracts;
using CanadaDeals.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanadaDeals.Api.Controllers;

[ApiController]
[Route("api/internal/listing-issue-reports")]
public sealed class InternalListingIssueReportsController(DealsDbContext db, IHostEnvironment environment) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<InternalListingIssueReportResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromQuery] string? status, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment()) return NotFound();

        if (!ListingIssueReportContractValues.TryParseStatus(status, out var parsedStatus))
        {
            ModelState.AddModelError(nameof(status), "Choose OPEN, REVIEWED, RESOLVED, or DISMISSED.");
            return ValidationProblem(ModelState);
        }

        var reports = await db.ListingIssueReports
            .AsNoTracking()
            .Where(x => x.Status == parsedStatus)
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .Select(x => new
            {
                x.Id,
                x.RetailerListingId,
                Retailer = x.RetailerListing.Retailer.Name,
                ListingTitle = x.RetailerListing.OriginalTitle,
                x.Reason,
                x.Note,
                x.Status,
                x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(reports.Select(x => new InternalListingIssueReportResponse(
            x.Id,
            x.RetailerListingId,
            x.Retailer,
            x.ListingTitle,
            x.Reason.ToContract(),
            x.Note,
            x.Status.ToContract(),
            x.CreatedAt)));
    }
}
