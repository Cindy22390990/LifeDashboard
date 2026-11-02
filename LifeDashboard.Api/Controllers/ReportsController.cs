using LifeDashboard.Api.Extensions;
using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeDashboard.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("monthly")]
    public async Task<ActionResult<MonthlyReportDto>> GetMonthlyReport(
        [FromQuery] int year,
        [FromQuery] int month)
    {
        try
        {
            var userId = User.GetUserId();
            var report = await _reportService.GetMonthlyReportAsync(
                userId,
                year,
                month);

            return Ok(report);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
