using CanadaDeals.Api.Contracts;
using CanadaDeals.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CanadaDeals.Api.Controllers;

[ApiController]
[Route("api/v1/deals")]
public sealed class DiscoveryController(CatalogQueryService catalog) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<DiscoveryResponse>(StatusCodes.Status200OK)]
    public Task<DiscoveryResponse> Get([FromQuery] string? search, CancellationToken cancellationToken) =>
        catalog.GetDiscoveryAsync(search, cancellationToken);
}
