using CanadaDeals.Api.Contracts;
using CanadaDeals.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CanadaDeals.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
public sealed class ProductsController(CatalogQueryService catalog) : ControllerBase
{
    [HttpGet("{slug}")]
    [ProducesResponseType<ProductDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string slug, CancellationToken cancellationToken)
    {
        var result = await catalog.GetProductAsync(slug, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
