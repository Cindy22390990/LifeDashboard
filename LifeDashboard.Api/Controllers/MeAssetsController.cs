using LifeDashboard.Api.Extensions;
using LifeDashboard.Application.Features.V4.Services;
using LifeDashboard.Application.Features.V4.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeDashboard.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public sealed class MeAssetsController : ControllerBase
{
    private readonly IV4MeAssetsService _meAssetsService;

    public MeAssetsController(IV4MeAssetsService meAssetsService)
    {
        _meAssetsService = meAssetsService;
    }

    [HttpGet("assets")]
    [ProducesResponseType(typeof(MeAssetsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MeAssetsDto>> GetAssets(
        CancellationToken cancellationToken)
    {
        var result = await _meAssetsService.GetMeAssetsAsync(
            User.GetUserId(),
            cancellationToken);

        return Ok(result);
    }
}
