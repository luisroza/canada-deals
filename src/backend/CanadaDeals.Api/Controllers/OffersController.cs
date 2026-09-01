using CanadaDeals.Api.Contracts;
using CanadaDeals.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CanadaDeals.Api.Controllers;

[ApiController]
[Route("api/v1/offers")]
public sealed class OffersController(CatalogQueryService catalog) : ControllerBase
{
    [HttpGet("{listingId:guid}")]
    [ProducesResponseType<ProductDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid listingId, CancellationToken cancellationToken)
    {
        var result = await catalog.GetOfferAsync(listingId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
