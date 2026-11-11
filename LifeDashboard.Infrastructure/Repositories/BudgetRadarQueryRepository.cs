using LifeDashboard.Application.Models.Queries;
using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Domain.Enums;
using LifeDashboard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LifeDashboard.Infrastructure.Repositories;

public sealed class BudgetRadarQueryRepository : IBudgetRadarQueryRepository
{
    private readonly LifeDashboardDbContext _context;

    public BudgetRadarQueryRepository(LifeDashboardDbContext context)
    {
        _context = context;
    }

    public async Task<BudgetExpenseSummary> GetExpenseSummaryAsync(
        Guid userId,
        DateOnly periodStartDate,
        DateOnly periodEndDate,
        DateOnly asOfDate)
    {
        var expenses = _context.Transactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.UserId == userId &&
                transaction.Type == TransactionType.Expense &&
                transaction.TransactionDate >= periodStartDate &&
                transaction.TransactionDate <= periodEndDate)
            .Where(transaction => !transaction.GoalId.HasValue ||
                !_context.Goals.Any(goal =>
                    goal.Id == transaction.GoalId &&
                    goal.UserId == userId &&
                    goal.GoalType == GoalType.Purchase));

        var periodLivingExpense = await expenses
            .Where(transaction =>
                transaction.CategoryEntity != null &&
                transaction.CategoryEntity.BucketType == BucketType.Living)
            .SumAsync(transaction => (decimal?)transaction.Amount) ?? 0m;

        var todayLivingExpense = await expenses
            .Where(transaction =>
                transaction.TransactionDate == asOfDate &&
                transaction.CategoryEntity != null &&
                transaction.CategoryEntity.BucketType == BucketType.Living)
            .SumAsync(transaction => (decimal?)transaction.Amount) ?? 0m;

        var uncategorizedExpenseTotal = await expenses
            .Where(transaction => transaction.CategoryId == null)
            .SumAsync(transaction => (decimal?)transaction.Amount) ?? 0m;

        return new BudgetExpenseSummary(
            periodLivingExpense,
            todayLivingExpense,
            uncategorizedExpenseTotal);
    }

    public async Task<GoalPurchaseAggregate?> GetGoalPurchaseAggregateAsync(
        Guid userId,
        DateOnly periodStartDate,
        DateOnly periodEndDate)
    {
        var purchaseExpenses = _context.Transactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.UserId == userId &&
                transaction.Type == TransactionType.Expense &&
                transaction.TransactionDate >= periodStartDate &&
                transaction.TransactionDate <= periodEndDate &&
                transaction.GoalId.HasValue)
            .Where(transaction => _context.Goals.Any(goal =>
                goal.Id == transaction.GoalId &&
                goal.UserId == userId &&
                goal.GoalType == GoalType.Purchase));

        var aggregate = await purchaseExpenses
            .GroupBy(_ => 1)
            .Select(group => new
            {
                ExpenseTotal = group.Sum(transaction => transaction.Amount),
                TransactionCount = group.Count()
            })
            .SingleOrDefaultAsync();

        return aggregate is null
            ? new GoalPurchaseAggregate(0m, 0)
            : new GoalPurchaseAggregate(aggregate.ExpenseTotal, aggregate.TransactionCount);
    }

}