namespace LifeDashboard.Application.Features.Dashboard.Dtos;

public sealed record DashboardCalendarMonthDto(
    int Year,
    int Month,
    IReadOnlyList<CalendarDaySummaryDto> Days);
