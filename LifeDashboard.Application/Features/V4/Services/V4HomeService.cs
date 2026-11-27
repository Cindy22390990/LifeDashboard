using LifeDashboard.Application.Features.V4.Dtos;
using LifeDashboard.Application.Features.V4.Interfaces;

namespace LifeDashboard.Application.Features.V4.Services;

public sealed class V4HomeService : IV4HomeService
{
    private readonly IV4HomeReader _reader;
    private readonly IDailyBudgetStrategy _dailyBudgetStrategy;

    public V4HomeService(
        IV4HomeReader reader,
        IDailyBudgetStrategy dailyBudgetStrategy)
    {
        _reader = reader;
        _dailyBudgetStrategy = dailyBudgetStrategy;
    }

    public async Task<DashboardV4HomeDto> GetHomeAsync(
        Guid userId,
        DateOnly businessDate,
        CancellationToken cancellationToken)
    {
        var projection = await _reader.GetHomeProjectionAsync(
            userId,
            businessDate,
            cancellationToken);

        var budget = _dailyBudgetStrategy.Calculate(new DailyBudgetContext(
            projection.AvailableMonthlyBudget,
            projection.ReservedFutureCommitments,
            projection.AccumulatedExpenseThroughYesterday,
            projection.DailySpent,
            projection.DaysInMonth,
            projection.ElapsedDays,
            projection.RemainingDays));

        var todayTransactions = projection.TodayTransactions
            .Select(transaction => new V4HomeTransactionItemDto(
                transaction.Id,
                transaction.Note,
                transaction.Amount,
                transaction.TransactionType.ToString(),
                transaction.TransactionDate))
            .ToArray();

        return new DashboardV4HomeDto(
            businessDate,
            budget.DailySpendingLimit,
            budget.DailySpent,
            budget.DailyRemaining,
            new MonthlyFinancialProgressDto(
                projection.MonthlyProgress.SavedAmount,
                projection.MonthlyProgress.InvestedAmount,
                projection.MonthlyProgress.Source),
            todayTransactions);
    }
}
