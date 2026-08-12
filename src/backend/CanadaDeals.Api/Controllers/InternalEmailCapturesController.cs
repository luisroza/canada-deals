using CanadaDeals.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanadaDeals.Api.Controllers;

[ApiController]
[Route("api/internal/email-captures")]
public sealed class InternalEmailCapturesController(DealsDbContext db, IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet("latest")]
    [AllowAnonymous]
    public async Task<IActionResult> Latest([FromQuery] string to, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Test")) return NotFound();
        if (string.IsNullOrWhiteSpace(to) || to.Length > 254) return BadRequest();
        var capture = await db.ControlledEmailCaptures.AsNoTracking()
            .Where(x => x.DestinationAddress == to)
            .OrderByDescending(x => x.CapturedAt)
            .Select(x => new { x.Subject, x.HtmlBody, x.TextBody, x.CapturedAt })
            .FirstOrDefaultAsync(cancellationToken);
        return capture is null ? NotFound() : Ok(capture);
    }
}
