using LifeDashboard.Domain.ValueObjects;

namespace LifeDashboard.Tests.BudgetRadar;

public class BudgetSnapshotTests
{
    [Fact]
    public void Calculate_applies_payroll_budget_formula_and_inclusive_remaining_days()
    {
        var snapshot = BudgetSnapshot.Calculate(
            new DateOnly(2026, 11, 2),
            new DateOnly(2026, 10, 5),
            new DateOnly(2026, 11, 4),
            periodIncome: 10_000m,
            fixedCommitmentTotal: 1_000m,
            periodLivingExpense: 1_250m,
            uncategorizedExpenseTotal: 200m,
            todayLivingExpense: 500m);

        Assert.Equal(new DateOnly(2026, 11, 2), snapshot.AsOfDate);
        Assert.Equal(new DateOnly(2026, 10, 5), snapshot.PeriodStartDate);
        Assert.Equal(new DateOnly(2026, 11, 4), snapshot.PeriodEndDate);
        Assert.Equal(new DateOnly(2026, 10, 1), snapshot.PayrollPeriodMonth);
        Assert.Equal(9_000m, snapshot.TotalLivingBudget);
        Assert.Equal(7_750m, snapshot.RemainingLivingBudget);
        Assert.Equal(3, snapshot.RemainingDays);
        Assert.Equal(2_583.33m, snapshot.DailySafeLimit);
        Assert.Equal(500m, snapshot.TodayLivingExpense);
        Assert.Equal(2_083.33m, snapshot.TodayRemaining);
        Assert.Equal(200m, snapshot.UncategorizedExpenseTotal);
        Assert.Null(snapshot.GoalPurchaseExpenseTotal);
        Assert.Null(snapshot.GoalPurchaseCount);
    }

    [Fact]
    public void Calculate_preserves_negative_budget_and_daily_amounts()
    {
        var snapshot = BudgetSnapshot.Calculate(
            new DateOnly(2026, 11, 2),
            new DateOnly(2026, 10, 5),
            new DateOnly(2026, 11, 4),
            periodIncome: 100m,
            fixedCommitmentTotal: 200m,
            periodLivingExpense: 10m,
            uncategorizedExpenseTotal: 0m,
            todayLivingExpense: 5m);

        Assert.Equal(-100m, snapshot.TotalLivingBudget);
        Assert.Equal(-110m, snapshot.RemainingLivingBudget);
        Assert.Equal(-36.67m, snapshot.DailySafeLimit);
        Assert.Equal(-41.67m, snapshot.TodayRemaining);
    }

    [Theory]
    [InlineData(1.015, 0, 0.51)]
    [InlineData(0, 1.015, -0.51)]
    public void Calculate_rounds_daily_limit_away_from_zero(
        decimal income,
        decimal commitments,
        decimal expectedDailyLimit)
    {
        var snapshot = BudgetSnapshot.Calculate(
            new DateOnly(2026, 11, 3),
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 11, 4),
            periodIncome: income,
            fixedCommitmentTotal: commitments,
            periodLivingExpense: 0m,
            uncategorizedExpenseTotal: 0m,
            todayLivingExpense: 0m);

        Assert.Equal(expectedDailyLimit, snapshot.DailySafeLimit);
    }

    [Fact]
    public void Calculate_keeps_goal_purchase_metrics_independent_from_living_budget()
    {
        var snapshot = BudgetSnapshot.Calculate(
            new DateOnly(2026, 11, 2),
            new DateOnly(2026, 10, 5),
            new DateOnly(2026, 11, 4),
            periodIncome: 1_000m,
            fixedCommitmentTotal: 100m,
            periodLivingExpense: 200m,
            uncategorizedExpenseTotal: 0m,
            todayLivingExpense: 0m,
            goalPurchaseExpenseTotal: 75m,
            goalPurchaseCount: 1);

        Assert.Equal(700m, snapshot.RemainingLivingBudget);
        Assert.Equal(75m, snapshot.GoalPurchaseExpenseTotal);
        Assert.Equal(1, snapshot.GoalPurchaseCount);
    }

    [Fact]
    public void Calculate_rejects_as_of_date_outside_period()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BudgetSnapshot.Calculate(
            new DateOnly(2026, 11, 5),
            new DateOnly(2026, 10, 5),
            new DateOnly(2026, 11, 4),
            periodIncome: 0m,
            fixedCommitmentTotal: 0m,
            periodLivingExpense: 0m,
            uncategorizedExpenseTotal: 0m,
            todayLivingExpense: 0m));
    }

    [Fact]
    public void Calculate_requires_goal_purchase_amount_and_count_together()
    {
        Assert.Throws<ArgumentException>(() => BudgetSnapshot.Calculate(
            new DateOnly(2026, 11, 2),
            new DateOnly(2026, 10, 5),
            new DateOnly(2026, 11, 4),
            periodIncome: 0m,
            fixedCommitmentTotal: 0m,
            periodLivingExpense: 0m,
            uncategorizedExpenseTotal: 0m,
            todayLivingExpense: 0m,
            goalPurchaseExpenseTotal: 100m));
    }
}