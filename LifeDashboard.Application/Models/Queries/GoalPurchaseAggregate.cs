namespace LifeDashboard.Application.Models.Queries;

public sealed record GoalPurchaseAggregate(
    decimal ExpenseTotal,
    int TransactionCount);