using LifeDashboard.Api.Extensions;
using LifeDashboard.Application.Features.Dashboard.Queries;
using LifeDashboard.Application.Features.V4.Dtos;
using LifeDashboard.Application.Features.V4.Services;
using DashboardCalendarMonthDto = LifeDashboard.Application.Features.Dashboard.Dtos.DashboardCalendarMonthDto;
using DashboardDayDetailDto = LifeDashboard.Application.Features.Dashboard.Dtos.DashboardDayDetailDto;
using V3DashboardSummaryDto = LifeDashboard.Application.Features.Dashboard.Dtos.DashboardSummaryDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeDashboard.Api.Controllers;

[ApiController]
[Route("api/dashboard/summary")]
[Authorize]
public sealed class DashboardController : ControllerBase
{
    private readonly GetDashboardSummaryQueryHandler _summaryQueryHandler;
    private readonly GetDashboardCalendarDayQueryHandler _calendarDayQueryHandler;
    private readonly GetDashboardCalendarMonthQueryHandler _calendarMonthQueryHandler;
    private readonly IV4DashboardSummaryService _v4SummaryService;

    public DashboardController(
        GetDashboardSummaryQueryHandler summaryQueryHandler,
        GetDashboardCalendarDayQueryHandler calendarDayQueryHandler,
        GetDashboardCalendarMonthQueryHandler calendarMonthQueryHandler,
        IV4DashboardSummaryService v4SummaryService)
    {
        _summaryQueryHandler = summaryQueryHandler;
        _calendarDayQueryHandler = calendarDayQueryHandler;
        _calendarMonthQueryHandler = calendarMonthQueryHandler;
        _v4SummaryService = v4SummaryService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(DashboardSummaryReadDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryReadDto>> GetSummary(
        CancellationToken cancellationToken)
    {
        var result = await _v4SummaryService.GetSummaryAsync(
            User.GetUserId(),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("/api/dashboard/v3/summary")]
    public async Task<ActionResult<V3DashboardSummaryDto>> GetV3Summary(
        CancellationToken cancellationToken)
    {
        var query = new GetDashboardSummaryQuery(User.GetUserId());
        var result = await _summaryQueryHandler.HandleAsync(
            query,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("/api/dashboard/v3/calendar/day")]
    public async Task<ActionResult<DashboardDayDetailDto>> GetV3CalendarDay(
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        if (!date.HasValue)
        {
            return BadRequest(new { message = "date is required in yyyy-MM-dd format." });
        }

        var query = new GetDashboardCalendarDayQuery(
            User.GetUserId(),
            date.Value);
        var result = await _calendarDayQueryHandler.HandleAsync(
            query,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("/api/dashboard/v3/calendar")]
    public async Task<ActionResult<DashboardCalendarMonthDto>> GetV3CalendarMonth(
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken cancellationToken)
    {
        if (!year.HasValue || year.Value is < 1 or > 9999)
        {
            return BadRequest(new { message = "year must be between 1 and 9999." });
        }

        if (!month.HasValue || month.Value is < 1 or > 12)
        {
            return BadRequest(new { message = "month must be between 1 and 12." });
        }

        var query = new GetDashboardCalendarMonthQuery(
            User.GetUserId(),
            year.Value,
            month.Value);
        var result = await _calendarMonthQueryHandler.HandleAsync(
            query,
            cancellationToken);

        return Ok(result);
    }
}