namespace LifeDashboard.Application.Features.Dashboard.Models;

public sealed record DashboardCalendarMonthProjection(
    int Year,
    int Month,
    IReadOnlyList<CalendarDaySummaryProjection> Days);
