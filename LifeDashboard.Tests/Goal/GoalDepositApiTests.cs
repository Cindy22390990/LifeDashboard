using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Domain.Enums;
using LifeDashboard.Infrastructure.Data;
using LifeDashboard.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GoalEntity = LifeDashboard.Domain.Entities.Goal;

namespace LifeDashboard.Tests.Goal;

public class GoalDepositApiTests
{
    [Fact]
    public async Task Deposit_uses_authenticated_user_and_persists_transfer_and_goal_update()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        await factory.InitializeDatabaseAsync();

        var user = new User("GoalDepositUser", "test-hash");
        var goal = new GoalEntity(
            user.Id,
            "Emergency savings",
            50m,
            GoalTier.Tier1_Safety,
            GoalType.Saving,
            string.Empty);
        var sourceAccount = new Account(
            user.Id,
            "Checking",
            AccountType.Cashflow,
            100m)
        {
            CurrentBalance = 100m
        };
        var destinationAccount = new Account(
            user.Id,
            "Savings",
            AccountType.Cashflow,
            0m);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            dbContext.Users.Add(user);
            dbContext.Goals.Add(goal);
            dbContext.Accounts.AddRange(sourceAccount, destinationAccount);
            await dbContext.SaveChangesAsync();
        }

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                CustomWebApplicationFactory.CreateJwt(user.Id));

        var response = await client.PostAsJsonAsync(
            $"/api/goals/{goal.Id}/deposit",
            new GoalDepositRequest
            {
                Amount = 75m,
                FromAccountId = sourceAccount.Id,
                ToAccountId = destinationAccount.Id,
                Note = "Emergency fund"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GoalDto>();
        Assert.NotNull(result);
        Assert.Equal(75m, result.CurrentAmount);
        Assert.Equal(GoalStatus.Completed, result.Status);
        Assert.NotNull(result.CompletedDate);

        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDbContext = verifyScope.ServiceProvider
            .GetRequiredService<LifeDashboardDbContext>();
        var persistedGoal = await verifyDbContext.Goals
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == goal.Id);
        var persistedTransaction = await verifyDbContext.Transactions
            .AsNoTracking()
            .SingleAsync(entity => entity.GoalId == goal.Id);
        var persistedSource = await verifyDbContext.Accounts
            .AsNoTracking()
            .SingleAsync(account => account.Id == sourceAccount.Id);
        var persistedDestination = await verifyDbContext.Accounts
            .AsNoTracking()
            .SingleAsync(account => account.Id == destinationAccount.Id);

        Assert.Equal(75m, persistedGoal.CurrentAmount);
        Assert.Equal(TransactionType.Transfer, persistedTransaction.Type);
        Assert.Equal(sourceAccount.Id, persistedTransaction.AccountId);
        Assert.Equal(destinationAccount.Id, persistedTransaction.ToAccountId);
        Assert.Equal(75m, persistedTransaction.Amount);
        Assert.Equal("Emergency fund", persistedTransaction.Note);
        Assert.Equal(25m, persistedSource.CurrentBalance);
        Assert.Equal(75m, persistedDestination.CurrentBalance);
    }

    [Fact]
    public async Task Deposit_without_jwt_returns_unauthorized()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/goals/1/deposit",
            new
            {
                amount = 25m,
                fromAccountId = Guid.NewGuid(),
                toAccountId = Guid.NewGuid(),
                note = "Unauthorized"
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Deposit_rejects_insufficient_source_balance_without_partial_changes()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        await factory.InitializeDatabaseAsync();

        var user = new User("GoalDepositInsufficientUser", "test-hash");
        var goal = new GoalEntity(
            user.Id,
            "Emergency savings",
            50m,
            GoalTier.Tier1_Safety,
            GoalType.Saving,
            string.Empty);
        var sourceAccount = CreateAccount(user.Id, "Checking", 10m);
        var destinationAccount = CreateAccount(user.Id, "Savings", 0m);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            dbContext.Users.Add(user);
            dbContext.Goals.Add(goal);
            dbContext.Accounts.AddRange(sourceAccount, destinationAccount);
            await dbContext.SaveChangesAsync();
        }

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                CustomWebApplicationFactory.CreateJwt(user.Id));
        var response = await client.PostAsJsonAsync(
            $"/api/goals/{goal.Id}/deposit",
            new GoalDepositRequest
            {
                Amount = 75m,
                FromAccountId = sourceAccount.Id,
                ToAccountId = destinationAccount.Id,
                Note = "Insufficient balance"
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
        Assert.Equal(10m, (await verifyDbContext.Accounts
            .AsNoTracking()
            .SingleAsync(account => account.Id == sourceAccount.Id)).CurrentBalance);
        Assert.Equal(0m, (await verifyDbContext.Accounts
            .AsNoTracking()
            .SingleAsync(account => account.Id == destinationAccount.Id)).CurrentBalance);
        Assert.Equal(0m, (await verifyDbContext.Goals
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == goal.Id)).CurrentAmount);
        Assert.Empty(await verifyDbContext.Transactions.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task Deposit_can_make_source_account_negative_when_enabled()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        await factory.InitializeDatabaseAsync();

        var user = new User("GoalDepositOverdraftUser", "test-hash");
        var goal = new GoalEntity(
            user.Id,
            "Emergency savings",
            50m,
            GoalTier.Tier1_Safety,
            GoalType.Saving,
            string.Empty);
        var sourceAccount = new Account(
            user.Id,
            "Checking",
            AccountType.Cashflow,
            10m,
            allowNegativeBalance: true)
        {
            CurrentBalance = 10m
        };
        var destinationAccount = CreateAccount(user.Id, "Savings", 0m);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            dbContext.Users.Add(user);
            dbContext.Goals.Add(goal);
            dbContext.Accounts.AddRange(sourceAccount, destinationAccount);
            await dbContext.SaveChangesAsync();
        }

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                CustomWebApplicationFactory.CreateJwt(user.Id));
        var response = await client.PostAsJsonAsync(
            $"/api/goals/{goal.Id}/deposit",
            new GoalDepositRequest
            {
                Amount = 75m,
                FromAccountId = sourceAccount.Id,
                ToAccountId = destinationAccount.Id,
                Note = "Allowed overdraft"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
        Assert.Equal(-65m, (await verifyDbContext.Accounts
            .AsNoTracking()
            .SingleAsync(account => account.Id == sourceAccount.Id)).CurrentBalance);
        Assert.Equal(75m, (await verifyDbContext.Accounts
            .AsNoTracking()
            .SingleAsync(account => account.Id == destinationAccount.Id)).CurrentBalance);
        Assert.Equal(75m, (await verifyDbContext.Goals
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == goal.Id)).CurrentAmount);
        Assert.Single(await verifyDbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.GoalId == goal.Id)
            .ToListAsync());
    }

    private static Account CreateAccount(Guid userId, string name, decimal balance)
    {
        return new Account(userId, name, AccountType.Cashflow, balance)
        {
            CurrentBalance = balance
        };
    }
}
