namespace LifeDashboard.Application.Features.Dashboard.Models;

public sealed record DashboardCalendarDayProjection(
    DateOnly Date,
    IReadOnlyList<CalendarTransactionProjection> Transactions);
