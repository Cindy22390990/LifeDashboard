using LifeDashboard.Api.Extensions;
using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeDashboard.Api.Controllers;

[ApiController]
[Route("api/budget/radar")]
[Authorize]
public class BudgetRadarController : ControllerBase
{
    private readonly IBudgetRadarService _budgetRadarService;

    public BudgetRadarController(IBudgetRadarService budgetRadarService)
    {
        _budgetRadarService = budgetRadarService;
    }

    [HttpGet]
    public async Task<ActionResult<RadarStateDto>> GetRadarState(
        [FromQuery] DateOnly? currentDate)
    {
        if (!currentDate.HasValue)
        {
            return BadRequest(new { message = "currentDate is required in yyyy-MM-dd format." });
        }

        try
        {
            var userId = User.GetUserId();
            return Ok(await _budgetRadarService.GetRadarStateAsync(userId, currentDate.Value));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }
}
