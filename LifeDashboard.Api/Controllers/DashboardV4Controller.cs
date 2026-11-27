using LifeDashboard.Api.Extensions;
using LifeDashboard.Application.Features.V4.Services;
using LifeDashboard.Application.Features.V4.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeDashboard.Api.Controllers;

[ApiController]
[Route("api/dashboard/v4")]
[Authorize]
public sealed class DashboardV4Controller : ControllerBase
{
    private readonly IV4HomeService _homeService;

    public DashboardV4Controller(IV4HomeService homeService)
    {
        _homeService = homeService;
    }

    [HttpGet("home")]
    [ProducesResponseType(typeof(DashboardV4HomeDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardV4HomeDto>> GetHome(
        CancellationToken cancellationToken)
    {
        var businessDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await _homeService.GetHomeAsync(
            User.GetUserId(),
            businessDate,
            cancellationToken);

        return Ok(result);
    }
}
