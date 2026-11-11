using LifeDashboard.Api.Extensions;
using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeDashboard.Api.Controllers;

[ApiController]
[Route("api/dashboard/summary")]
[Authorize]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(
        [FromQuery] DateOnly? currentDate,
        CancellationToken cancellationToken)
    {
        if (!currentDate.HasValue)
        {
            return BadRequest(new { message = "currentDate is required in yyyy-MM-dd format." });
        }

        var readModel = await _dashboardService.GetSummaryAsync(
            User.GetUserId(),
            currentDate.Value,
            cancellationToken);

        return Ok(new DashboardSummaryDto
        {
            Budget = readModel.Budget,
            Accounts = readModel.Accounts,
            Goals = readModel.Goals
        });
    }
}