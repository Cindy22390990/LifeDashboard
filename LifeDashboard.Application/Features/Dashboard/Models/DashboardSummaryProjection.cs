namespace LifeDashboard.Application.Features.Dashboard.Models;

public sealed record DashboardSummaryProjection(
    decimal TotalAssets,
    decimal TodayExpense,
    DateOnly BusinessDate,
    IReadOnlyList<DashboardAccountProjection> Accounts,
    IReadOnlyList<RecentTransactionProjection> RecentTransactions);
