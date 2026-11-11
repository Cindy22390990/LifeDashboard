namespace LifeDashboard.Application.Models.Queries;

public sealed record BudgetExpenseSummary(
    decimal PeriodLivingExpense,
    decimal TodayLivingExpense,
    decimal UncategorizedExpenseTotal);