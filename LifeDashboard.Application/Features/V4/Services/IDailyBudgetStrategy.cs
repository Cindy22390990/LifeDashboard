namespace LifeDashboard.Application.Features.V4.Services;

public sealed record DailyBudgetContext(
    decimal AvailableMonthlyBudget,
    decimal ReservedFutureCommitments,
    decimal AccumulatedExpenseThroughYesterday,
    decimal DailySpent,
    int DaysInMonth,
    int ElapsedDays,
    int RemainingDays);

public sealed record DailyBudgetResult(
    decimal DailySpendingLimit,
    decimal DailySpent,
    decimal DailyRemaining);

public interface IDailyBudgetStrategy
{
    DailyBudgetResult Calculate(DailyBudgetContext context);
}
