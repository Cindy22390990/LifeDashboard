using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Domain.Enums;
using LifeDashboard.Infrastructure.Data;
using LifeDashboard.Infrastructure.Repositories;
using LifeDashboard.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GoalEntity = LifeDashboard.Domain.Entities.Goal;
using TransactionEntity = LifeDashboard.Domain.Entities.Transaction;

namespace LifeDashboard.Tests.BudgetRadar;

public class BudgetRadarQueryRepositoryTests
{
    [Fact]
    public async Task Expense_and_goal_purchase_queries_scope_user_period_and_classification()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        var userA = new User("BudgetQueryUserA", "test-hash");
        var userB = new User("BudgetQueryUserB", "test-hash");
        var accountA = CreateAccount(userA.Id, "A account");
        var accountB = CreateAccount(userB.Id, "B account");
        var livingCategory = new Category(userA.Id, "Living", "home", BucketType.Living);
        var responsibilityCategory = new Category(
            userA.Id,
            "Responsibility",
            "bill",
            BucketType.Responsibility);
        var purchaseGoalA = CreateGoal(userA.Id, GoalType.Purchase, "A purchase goal");
        var savingGoalA = CreateGoal(userA.Id, GoalType.Saving, "A saving goal");
        var purchaseGoalB = CreateGoal(userB.Id, GoalType.Purchase, "B purchase goal");

        var transactions = new[]
        {
            CreateTransaction(userA.Id, accountA.Id, 60m, TransactionType.Expense,
                new DateOnly(2026, 10, 10), livingCategory),
            CreateTransaction(userA.Id, accountA.Id, 40m, TransactionType.Expense,
                new DateOnly(2026, 11, 2), livingCategory),
            CreateTransaction(userA.Id, accountA.Id, 30m, TransactionType.Expense,
                new DateOnly(2026, 11, 2), responsibilityCategory),
            CreateTransaction(userA.Id, accountA.Id, 25m, TransactionType.Expense,
                new DateOnly(2026, 11, 2)),
            CreateTransaction(userA.Id, accountA.Id, 500m, TransactionType.Expense,
                new DateOnly(2026, 11, 2), livingCategory, purchaseGoalA),
            CreateTransaction(userA.Id, accountA.Id, 100m, TransactionType.Expense,
                new DateOnly(2026, 11, 2), goal: purchaseGoalA),
            CreateTransaction(userA.Id, accountA.Id, 1_000m, TransactionType.Expense,
                new DateOnly(2026, 11, 5), livingCategory, purchaseGoalA),
            CreateTransaction(userA.Id, accountA.Id, 7m, TransactionType.Expense,
                new DateOnly(2026, 11, 2), livingCategory, purchaseGoalB),
            CreateTransaction(userA.Id, accountA.Id, 900m, TransactionType.Income,
                new DateOnly(2026, 11, 2), livingCategory),
            CreateTransaction(userA.Id, accountA.Id, 800m, TransactionType.Transfer,
                new DateOnly(2026, 11, 2), livingCategory),
            CreateTransaction(userB.Id, accountB.Id, 700m, TransactionType.Expense,
                new DateOnly(2026, 11, 2), new Category(userB.Id, "B Living", "home", BucketType.Living)),
            CreateTransaction(userB.Id, accountB.Id, 600m, TransactionType.Expense,
                new DateOnly(2026, 11, 2), goal: purchaseGoalB)
        };

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            dbContext.Users.AddRange(userA, userB);
            dbContext.Accounts.AddRange(accountA, accountB);
            dbContext.Categories.AddRange(livingCategory, responsibilityCategory);
            dbContext.Goals.AddRange(purchaseGoalA, savingGoalA, purchaseGoalB);
            dbContext.Transactions.AddRange(transactions);
            await dbContext.SaveChangesAsync();
        }

        await using var queryScope = factory.Services.CreateAsyncScope();
        var queryDbContext = queryScope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
        var queryRepository = new BudgetRadarQueryRepository(queryDbContext);

        var expenseSummary = await queryRepository.GetExpenseSummaryAsync(
            userA.Id,
            new DateOnly(2026, 10, 5),
            new DateOnly(2026, 11, 4),
            new DateOnly(2026, 11, 2));
        var purchaseAggregate = await queryRepository.GetGoalPurchaseAggregateAsync(
            userA.Id,
            new DateOnly(2026, 10, 5),
            new DateOnly(2026, 11, 4));

        Assert.NotNull(purchaseAggregate);
        Assert.Equal(107m, expenseSummary.PeriodLivingExpense);
        Assert.Equal(47m, expenseSummary.TodayLivingExpense);
        Assert.Equal(25m, expenseSummary.UncategorizedExpenseTotal);
        Assert.Equal(600m, purchaseAggregate.ExpenseTotal);
        Assert.Equal(2, purchaseAggregate.TransactionCount);

        var emptyAggregate = await queryRepository.GetGoalPurchaseAggregateAsync(
            userA.Id,
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 30));
        Assert.NotNull(emptyAggregate);
        Assert.Equal(0m, emptyAggregate.ExpenseTotal);
        Assert.Equal(0, emptyAggregate.TransactionCount);
    }

    [Fact]
    public async Task Active_fixed_commitments_are_scoped_to_user_and_inclusive_period_month()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        var userA = new User("CommitmentQueryUserA", "test-hash");
        var userB = new User("CommitmentQueryUserB", "test-hash");
        var activeWithoutEnd = CreateCommitment(
            userA.Id,
            "Active without end",
            new DateOnly(2026, 8, 1),
            null);
        var endingInPeriod = CreateCommitment(
            userA.Id,
            "Ends in period",
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 10, 1));
        var endedBeforePeriod = CreateCommitment(
            userA.Id,
            "Already ended",
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 9, 1));
        var startsAfterPeriod = CreateCommitment(
            userA.Id,
            "Starts later",
            new DateOnly(2026, 11, 1),
            null);
        var otherUserCommitment = CreateCommitment(
            userB.Id,
            "Other user active",
            new DateOnly(2026, 7, 1),
            null);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            dbContext.Users.AddRange(userA, userB);
            dbContext.FixedCommitments.AddRange(
                activeWithoutEnd,
                endingInPeriod,
                endedBeforePeriod,
                startsAfterPeriod,
                otherUserCommitment);
            await dbContext.SaveChangesAsync();
        }

        await using var queryScope = factory.Services.CreateAsyncScope();
        var queryDbContext = queryScope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
        var repository = new FixedCommitmentRepository(queryDbContext);

        var active = await repository.GetActiveForPayrollPeriodAsync(
            userA.Id,
            new DateOnly(2026, 10, 1));

        Assert.Equal(2, active.Count());
        Assert.Contains(active, commitment => commitment.Id == activeWithoutEnd.Id);
        Assert.Contains(active, commitment => commitment.Id == endingInPeriod.Id);
        Assert.DoesNotContain(active, commitment => commitment.Id == endedBeforePeriod.Id);
        Assert.DoesNotContain(active, commitment => commitment.Id == startsAfterPeriod.Id);
        Assert.DoesNotContain(active, commitment => commitment.Id == otherUserCommitment.Id);
    }

    private static Account CreateAccount(Guid userId, string name)
    {
        return new Account(userId, name, AccountType.Cashflow, 0m);
    }

    private static GoalEntity CreateGoal(Guid userId, GoalType goalType, string name)
    {
        return new GoalEntity(
            userId,
            name,
            1_000m,
            GoalTier.Tier1_Safety,
            goalType,
            string.Empty);
    }

    private static TransactionEntity CreateTransaction(
        Guid userId,
        Guid accountId,
        decimal amount,
        TransactionType type,
        DateOnly date,
        Category? category = null,
        GoalEntity? goal = null)
    {
        var transaction = new TransactionEntity(
            userId,
            accountId,
            amount,
            type,
            "Budget query test",
            date);

        if (category is not null)
        {
            transaction.CategoryId = category.Id;
            transaction.CategoryEntity = category;
        }

        if (goal is not null)
        {
            transaction.AssociateWithGoal(goal);
        }

        return transaction;
    }

    private static FixedCommitment CreateCommitment(
        Guid userId,
        string name,
        DateOnly startMonth,
        DateOnly? endMonth)
    {
        return new FixedCommitment(
            userId,
            name,
            100m,
            CommitmentType.Installment,
            startMonth,
            endMonth);
    }
}