namespace LifeDashboard.Application.Features.Dashboard.Queries;

public sealed record GetDashboardCalendarMonthQuery(
    Guid UserId,
    int Year,
    int Month);
