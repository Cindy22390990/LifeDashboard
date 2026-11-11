using LifeDashboard.Application.Models.Queries;

namespace LifeDashboard.Application.Repositories.Interfaces;

public interface IBudgetRadarQueryRepository
{
    Task<BudgetExpenseSummary> GetExpenseSummaryAsync(
        Guid userId,
        DateOnly periodStartDate,
        DateOnly periodEndDate,
        DateOnly asOfDate);

    Task<GoalPurchaseAggregate?> GetGoalPurchaseAggregateAsync(
        Guid userId,
        DateOnly periodStartDate,
        DateOnly periodEndDate);
}