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
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DiscoveryResponse>> Get([FromQuery] DiscoveryQueryRequest request, CancellationToken cancellationToken)
    {
        var validation = await catalog.ValidateDiscoveryRequestAsync(request, cancellationToken);
        if (validation.Count > 0)
        {
            foreach (var (key, message) in validation) ModelState.AddModelError(key, message);
            return ValidationProblem(ModelState);
        }

        return Ok(await catalog.GetDiscoveryAsync(request, cancellationToken));
    }
}
