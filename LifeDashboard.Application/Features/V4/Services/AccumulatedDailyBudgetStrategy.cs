namespace LifeDashboard.Application.Features.V4.Services;

public sealed class AccumulatedDailyBudgetStrategy : IDailyBudgetStrategy
{
    public DailyBudgetResult Calculate(DailyBudgetContext context)
    {
        if (context.AvailableMonthlyBudget <= 0m)
        {
            return new DailyBudgetResult(
                DailySpendingLimit: 0m,
                DailySpent: context.DailySpent,
                DailyRemaining: -context.DailySpent);
        }

        var remainingDays = Math.Max(1, context.RemainingDays);
        var remainingFlexibleBudget = Math.Max(
            0m,
            context.AvailableMonthlyBudget
                - context.ReservedFutureCommitments
                - context.AccumulatedExpenseThroughYesterday);

        var daysInMonth = Math.Max(1, context.DaysInMonth);
        var baseDailyBudget = remainingFlexibleBudget / remainingDays;
        var monthStartBaseDailyBudget =
            (context.AvailableMonthlyBudget - context.ReservedFutureCommitments)
                / daysInMonth;
        var elapsedDays = Math.Max(0, context.ElapsedDays);
        var previousUnusedBudget = Math.Max(
            0m,
            (monthStartBaseDailyBudget * elapsedDays)
                - context.AccumulatedExpenseThroughYesterday);

        var todayAvailable = decimal.Min(
            remainingFlexibleBudget,
            baseDailyBudget + previousUnusedBudget);

        if (todayAvailable < 0m)
        {
            todayAvailable = 0m;
        }

        return new DailyBudgetResult(
            DailySpendingLimit: todayAvailable,
            DailySpent: context.DailySpent,
            DailyRemaining: todayAvailable - context.DailySpent);
    }
}
