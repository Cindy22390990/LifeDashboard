namespace LifeDashboard.Application.Features.Dashboard.Dtos;

public sealed record DashboardSummaryDto(
    decimal TotalAssets,
    decimal TodayExpense,
    DateOnly BusinessDate,
    IReadOnlyList<DashboardAccountDto> Accounts,
    IReadOnlyList<RecentTransactionDto> RecentTransactions);
