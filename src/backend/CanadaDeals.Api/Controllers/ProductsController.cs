using CanadaDeals.Api.Contracts;
using CanadaDeals.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CanadaDeals.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
public sealed class ProductsController(CatalogQueryService catalog, IConfiguration configuration) : ControllerBase
{
    [HttpGet("{slug}")]
    [ProducesResponseType<ProductDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string slug, CancellationToken cancellationToken)
    {
        var result = await catalog.GetProductAsync(slug, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{slug}/history")]
    [ProducesResponseType<ProductHistoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHistory(string slug, [FromQuery] string? window, CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("ProductFeatures:PriceHistoryEnabled")) return NotFound();
        if (!CanadaDeals.Domain.PriceTruth.ProductHistoryRules.TryParseWindow(window, out var parsedWindow))
        {
            ModelState.AddModelError(nameof(window), "History window must be 30d or 90d.");
            return ValidationProblem(ModelState);
        }

        var result = await catalog.GetProductHistoryAsync(slug, parsedWindow, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
