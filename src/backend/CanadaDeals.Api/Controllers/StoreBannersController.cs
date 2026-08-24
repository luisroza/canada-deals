using CanadaDeals.Api.Contracts;
using CanadaDeals.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CanadaDeals.Api.Controllers;

[ApiController]
[Route("api/v1/store-banners")]
public sealed class StoreBannersController(StoreBannerQueryService banners) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<StoreBannerResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StoreBannerResponse>>> Get(CancellationToken cancellationToken) =>
        Ok(await banners.GetAsync(cancellationToken));
}
