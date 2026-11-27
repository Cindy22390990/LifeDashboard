namespace LifeDashboard.Application.Features.V4.Dtos;

public sealed record DashboardSummaryReadDto(
    decimal TotalAssets,
    decimal MonthlyIncome,
    decimal MonthlyExpense);
