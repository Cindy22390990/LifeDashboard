namespace LifeDashboard.Application.Features.Dashboard.Dtos;

public sealed record CalendarDaySummaryDto(
    DateOnly Date,
    decimal TotalIncome,
    decimal TotalExpense,
    bool HasTransfer);
