using CanadaDeals.Domain.Catalog;
using CanadaDeals.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanadaDeals.Api.Controllers;

[ApiController]
[Route("api/v1/product-images")]
public sealed class ProductImagesController(DealsDbContext db, TimeProvider clock) : ControllerBase
{
    [HttpGet("{imageId:guid}")]
    public async Task<IActionResult> Get(Guid imageId, CancellationToken cancellationToken)
    {
        var image = await db.ProductImages.AsNoTracking().SingleOrDefaultAsync(item => item.Id == imageId, cancellationToken);
        if (image is null || !image.CanDisplay(clock.GetUtcNow(), "DEAL_CARD") &&
            !image.CanDisplay(clock.GetUtcNow(), "PRODUCT_PAGE") && !image.CanDisplay(clock.GetUtcNow(), "WISHLIST")) return NotFound();

        var etag = $"\"{image.ContentHash}\"";
        if (Request.Headers.IfNoneMatch.Contains(etag)) return StatusCode(StatusCodes.Status304NotModified);
        Response.Headers.ETag = etag;
        Response.Headers.CacheControl = "public,max-age=0,must-revalidate";
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(image.Content, image.ContentType);
    }
}
