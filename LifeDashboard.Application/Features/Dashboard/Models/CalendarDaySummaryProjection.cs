namespace LifeDashboard.Application.Features.Dashboard.Models;

public sealed record CalendarDaySummaryProjection(
    DateOnly Date,
    decimal TotalIncome,
    decimal TotalExpense,
    bool HasTransfer);
