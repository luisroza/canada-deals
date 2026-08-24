using CanadaDeals.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanadaDeals.Api.Controllers;

[ApiController]
[Route("api/v1/store-banner-assets")]
public sealed class StoreBannerAssetsController(DealsDbContext db) : ControllerBase
{
    [HttpGet("{assetId:guid}")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Get(Guid assetId, CancellationToken cancellationToken)
    {
        var asset = await db.StoreBannerAssets.AsNoTracking()
            .Where(item => item.Id == assetId)
            .Select(item => new { item.Id, item.ContentType, item.Content })
            .SingleOrDefaultAsync(cancellationToken);
        if (asset is null) return NotFound();

        Response.Headers.ETag = $"\"{asset.Id:N}\"";
        return File(asset.Content, asset.ContentType);
    }
}
