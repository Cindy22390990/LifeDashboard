using LifeDashboard.Application.Features.V4.Services;

namespace LifeDashboard.Tests.V4;

public sealed class DailyBudgetStrategyTests
{
    private readonly AccumulatedDailyBudgetStrategy _strategy = new();

    [Fact]
    public void Normal_calculation_returns_expected_remaining()
    {
        var result = _strategy.Calculate(new DailyBudgetContext(
            AvailableMonthlyBudget: 3_000m,
            ReservedFutureCommitments: 0m,
            AccumulatedExpenseThroughYesterday: 1_000m,
            DailySpent: 100m,
            DaysInMonth: 30,
            ElapsedDays: 10,
            RemainingDays: 20));

        Assert.Equal(0m, result.DailyRemaining);
        Assert.Equal(100m, result.DailySpendingLimit);
        Assert.Equal(100m, result.DailySpent);
    }

    [Fact]
    public void Zero_budget_returns_zero_limit_and_negative_daily_remaining()
    {
        var result = _strategy.Calculate(new DailyBudgetContext(
            0m,
            0m,
            0m,
            250m,
            30,
            10,
            20));

        Assert.Equal(0m, result.DailySpendingLimit);
        Assert.Equal(-250m, result.DailyRemaining);
    }

    [Fact]
    public void Last_day_of_month_locks_remaining_days_to_one()
    {
        var result = _strategy.Calculate(new DailyBudgetContext(
            310m,
            10m,
            300m,
            50m,
            31,
            30,
            1));

        Assert.True(result.DailySpendingLimit >= 0m);
    }

    [Fact]
    public void Overspending_keeps_negative_daily_remaining()
    {
        var result = _strategy.Calculate(new DailyBudgetContext(
            1_000m,
            0m,
            900m,
            300m,
            30,
            20,
            10));

        Assert.True(result.DailyRemaining < 0m);
    }

    [Fact]
    public void Accumulation_is_capped_by_remaining_flexible_budget()
    {
        var result = _strategy.Calculate(new DailyBudgetContext(
            1_000m,
            100m,
            100m,
            50m,
            30,
            10,
            20));

        Assert.True(result.DailySpendingLimit <= 800m);
    }
}
