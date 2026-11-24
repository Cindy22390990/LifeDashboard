namespace LifeDashboard.Application.Features.Dashboard.Queries;

public sealed record GetDashboardCalendarDayQuery(
    Guid UserId,
    DateOnly Date);
