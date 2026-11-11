namespace LifeDashboard.Domain.ValueObjects;

public sealed record BudgetSnapshot
{
    private BudgetSnapshot(
        DateOnly asOfDate,
        DateOnly periodStartDate,
        DateOnly periodEndDate,
        DateOnly payrollPeriodMonth,
        decimal periodIncome,
        decimal fixedCommitmentTotal,
        decimal totalLivingBudget,
        decimal periodLivingExpense,
        decimal remainingLivingBudget,
        decimal uncategorizedExpenseTotal,
        decimal? goalPurchaseExpenseTotal,
        int? goalPurchaseCount,
        int remainingDays,
        decimal dailySafeLimit,
        decimal todayLivingExpense,
        decimal todayRemaining)
    {
        AsOfDate = asOfDate;
        PeriodStartDate = periodStartDate;
        PeriodEndDate = periodEndDate;
        PayrollPeriodMonth = payrollPeriodMonth;
        PeriodIncome = periodIncome;
        FixedCommitmentTotal = fixedCommitmentTotal;
        TotalLivingBudget = totalLivingBudget;
        PeriodLivingExpense = periodLivingExpense;
        RemainingLivingBudget = remainingLivingBudget;
        UncategorizedExpenseTotal = uncategorizedExpenseTotal;
        GoalPurchaseExpenseTotal = goalPurchaseExpenseTotal;
        GoalPurchaseCount = goalPurchaseCount;
        RemainingDays = remainingDays;
        DailySafeLimit = dailySafeLimit;
        TodayLivingExpense = todayLivingExpense;
        TodayRemaining = todayRemaining;
    }

    public DateOnly AsOfDate { get; }
    public DateOnly PeriodStartDate { get; }
    public DateOnly PeriodEndDate { get; }
    public DateOnly PayrollPeriodMonth { get; }
    public decimal PeriodIncome { get; }
    public decimal FixedCommitmentTotal { get; }
    public decimal TotalLivingBudget { get; }
    public decimal PeriodLivingExpense { get; }
    public decimal RemainingLivingBudget { get; }
    public decimal UncategorizedExpenseTotal { get; }
    public decimal? GoalPurchaseExpenseTotal { get; }
    public int? GoalPurchaseCount { get; }
    public int RemainingDays { get; }
    public decimal DailySafeLimit { get; }
    public decimal TodayLivingExpense { get; }
    public decimal TodayRemaining { get; }

    public static BudgetSnapshot Calculate(
        DateOnly asOfDate,
        DateOnly periodStartDate,
        DateOnly periodEndDate,
        decimal periodIncome,
        decimal fixedCommitmentTotal,
        decimal periodLivingExpense,
        decimal uncategorizedExpenseTotal,
        decimal todayLivingExpense,
        decimal? goalPurchaseExpenseTotal = null,
        int? goalPurchaseCount = null)
    {
        if (periodStartDate > asOfDate || asOfDate > periodEndDate)
        {
            throw new ArgumentOutOfRangeException(
                nameof(asOfDate),
                "查詢日期必須位於薪資週期內。");
        }

        if (periodStartDate > periodEndDate)
        {
            throw new ArgumentException("薪資週期起日不可晚於迄日。", nameof(periodStartDate));
        }

        if (goalPurchaseExpenseTotal.HasValue != goalPurchaseCount.HasValue)
        {
            throw new ArgumentException("Goal Purchase 金額與筆數必須同時提供或同時省略。");
        }

        if (goalPurchaseExpenseTotal < 0 || goalPurchaseCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(goalPurchaseExpenseTotal),
                "Goal Purchase 統計不可為負數。");
        }

        var totalLivingBudget = periodIncome - fixedCommitmentTotal;
        var remainingLivingBudget = totalLivingBudget - periodLivingExpense;
        var remainingDays = periodEndDate.DayNumber - asOfDate.DayNumber + 1;
        var dailySafeLimit = decimal.Round(
            remainingLivingBudget / remainingDays,
            2,
            MidpointRounding.AwayFromZero);

        return new BudgetSnapshot(
            asOfDate,
            periodStartDate,
            periodEndDate,
            new DateOnly(periodStartDate.Year, periodStartDate.Month, 1),
            periodIncome,
            fixedCommitmentTotal,
            totalLivingBudget,
            periodLivingExpense,
            remainingLivingBudget,
            uncategorizedExpenseTotal,
            goalPurchaseExpenseTotal,
            goalPurchaseCount,
            remainingDays,
            dailySafeLimit,
            todayLivingExpense,
            dailySafeLimit - todayLivingExpense);
    }
}