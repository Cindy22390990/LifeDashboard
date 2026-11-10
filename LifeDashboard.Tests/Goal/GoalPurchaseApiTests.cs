using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LifeDashboard.Api;
using LifeDashboard.Application.Interfaces;
using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Domain.Enums;
using LifeDashboard.Infrastructure.Data;
using LifeDashboard.Tests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using GoalEntity = LifeDashboard.Domain.Entities.Goal;
using TransactionEntity = LifeDashboard.Domain.Entities.Transaction;

namespace LifeDashboard.Tests.Goal;

public class GoalPurchaseApiTests
{
    [Fact]
    public async Task Purchase_creates_expense_and_completes_goal_without_changing_current_amount()
    {
        using var factory = new PurchaseTestFactory();
        var seeded = await factory.SeedAsync();
        using var client = factory.CreateAuthenticatedClient(seeded.UserAId);

        var response = await PostPurchaseAsync(client, seeded.UserAGoal.Id, seeded.UserASource.Id, seeded.UserACategory.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var goalResponse = await response.Content.ReadFromJsonAsync<GoalDto>();
        Assert.NotNull(goalResponse);
        Assert.Equal(100m, goalResponse.CurrentAmount);
        Assert.Equal(GoalStatus.Completed, goalResponse.Status);
        Assert.NotNull(goalResponse.CompletedDate);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
        var transaction = await dbContext.Transactions
            .AsNoTracking()
            .SingleAsync();
        var goal = await dbContext.Goals
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == seeded.UserAGoal.Id);
        var account = await dbContext.Accounts
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == seeded.UserASource.Id);

        Assert.Equal(TransactionType.Expense, transaction.Type);
        Assert.Equal(seeded.UserAGoal.Id, transaction.GoalId);
        Assert.Equal(seeded.UserASource.Id, transaction.AccountId);
        Assert.Null(transaction.ToAccountId);
        Assert.Equal(seeded.UserACategory.Id, transaction.CategoryId);
        Assert.Equal(75m, transaction.Amount);
        Assert.Equal(25m, account.CurrentBalance);
        Assert.Equal(100m, goal.CurrentAmount);
        Assert.Equal(GoalStatus.Completed, goal.Status);
    }

    [Fact]
    public async Task Purchase_with_another_users_goal_is_rejected_without_changes()
    {
        using var factory = new PurchaseTestFactory();
        var seeded = await factory.SeedAsync();
        using var client = factory.CreateAuthenticatedClient(seeded.UserAId);

        var response = await PostPurchaseAsync(
            client,
            seeded.UserBGoal.Id,
            seeded.UserASource.Id,
            seeded.UserACategory.Id);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertNoPurchaseChangesAsync(factory, seeded);
    }

    [Fact]
    public async Task Purchase_for_non_purchase_goal_returns_conflict_without_changes()
    {
        using var factory = new PurchaseTestFactory();
        var seeded = await factory.SeedAsync(
            userAGoalType: GoalType.Saving,
            userAGoalCurrentAmount: 50m);
        using var client = factory.CreateAuthenticatedClient(seeded.UserAId);

        var response = await PostPurchaseAsync(
            client,
            seeded.UserAGoal.Id,
            seeded.UserASource.Id,
            seeded.UserACategory.Id);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertNoPurchaseChangesAsync(factory, seeded);
    }

    [Fact]
    public async Task Purchase_for_goal_below_target_creates_expense_and_completes_goal()
    {
        using var factory = new PurchaseTestFactory();
        var seeded = await factory.SeedAsync(userAGoalCurrentAmount: 50m);
        using var client = factory.CreateAuthenticatedClient(seeded.UserAId);

        var response = await PostPurchaseAsync(
            client,
            seeded.UserAGoal.Id,
            seeded.UserASource.Id,
            seeded.UserACategory.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
        var goal = await dbContext.Goals
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == seeded.UserAGoal.Id);
        var account = await dbContext.Accounts
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == seeded.UserASource.Id);

        Assert.Equal(50m, goal.CurrentAmount);
        Assert.Equal(GoalStatus.Completed, goal.Status);
        Assert.Equal(25m, account.CurrentBalance);
        Assert.Single(await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.GoalId == seeded.UserAGoal.Id)
            .ToListAsync());
    }

    [Fact]
    public async Task Purchase_for_completed_goal_returns_conflict_without_changes()
    {
        using var factory = new PurchaseTestFactory();
        var seeded = await factory.SeedAsync(completeUserAGoal: true);
        using var client = factory.CreateAuthenticatedClient(seeded.UserAId);

        var response = await PostPurchaseAsync(
            client,
            seeded.UserAGoal.Id,
            seeded.UserASource.Id,
            seeded.UserACategory.Id);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertNoPurchaseChangesAsync(factory, seeded);
    }

    [Fact]
    public async Task Purchase_with_another_users_account_is_rejected_without_changes()
    {
        using var factory = new PurchaseTestFactory();
        var seeded = await factory.SeedAsync();
        using var client = factory.CreateAuthenticatedClient(seeded.UserAId);

        var response = await PostPurchaseAsync(
            client,
            seeded.UserAGoal.Id,
            seeded.UserBSource.Id,
            seeded.UserACategory.Id);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertNoPurchaseChangesAsync(factory, seeded);
    }

    [Fact]
    public async Task Purchase_with_another_users_category_returns_not_found_without_changes()
    {
        using var factory = new PurchaseTestFactory();
        var seeded = await factory.SeedAsync();
        using var client = factory.CreateAuthenticatedClient(seeded.UserAId);

        var response = await PostPurchaseAsync(
            client,
            seeded.UserAGoal.Id,
            seeded.UserASource.Id,
            seeded.UserBCategory.Id);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertNoPurchaseChangesAsync(factory, seeded);
    }

    [Fact]
    public async Task Purchase_with_living_category_returns_bad_request_without_changes()
    {
        using var factory = new PurchaseTestFactory();
        var seeded = await factory.SeedAsync(userACategoryBucket: BucketType.Living);
        using var client = factory.CreateAuthenticatedClient(seeded.UserAId);

        var response = await PostPurchaseAsync(
            client,
            seeded.UserAGoal.Id,
            seeded.UserASource.Id,
            seeded.UserACategory.Id);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertNoPurchaseChangesAsync(factory, seeded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Purchase_with_non_positive_amount_returns_bad_request_without_changes(
        decimal actualAmount)
    {
        using var factory = new PurchaseTestFactory();
        var seeded = await factory.SeedAsync();
        using var client = factory.CreateAuthenticatedClient(seeded.UserAId);

        var response = await PostPurchaseAsync(
            client,
            seeded.UserAGoal.Id,
            seeded.UserASource.Id,
            seeded.UserACategory.Id,
            actualAmount);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertNoPurchaseChangesAsync(factory, seeded);
    }

    [Fact]
    public async Task Purchase_without_authentication_returns_unauthorized_without_changes()
    {
        using var factory = new PurchaseTestFactory();
        var seeded = await factory.SeedAsync();
        using var client = factory.CreateClient();

        var response = await PostPurchaseAsync(
            client,
            seeded.UserAGoal.Id,
            seeded.UserASource.Id,
            seeded.UserACategory.Id);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertNoPurchaseChangesAsync(factory, seeded);
    }

    [Fact]
    public async Task Purchase_with_insufficient_account_balance_is_rejected_without_changes()
    {
        using var factory = new PurchaseTestFactory();
        var seeded = await factory.SeedAsync(userASourceBalance: 50m);
        using var client = factory.CreateAuthenticatedClient(seeded.UserAId);

        var response = await PostPurchaseAsync(
            client,
            seeded.UserAGoal.Id,
            seeded.UserASource.Id,
            seeded.UserACategory.Id);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertNoPurchaseChangesAsync(factory, seeded);
    }

    [Fact]
    public async Task Purchase_can_make_account_negative_when_enabled()
    {
        using var factory = new PurchaseTestFactory();
        var seeded = await factory.SeedAsync(
            userASourceBalance: 10m,
            userASourceAllowNegativeBalance: true);
        using var client = factory.CreateAuthenticatedClient(seeded.UserAId);

        var response = await PostPurchaseAsync(
            client,
            seeded.UserAGoal.Id,
            seeded.UserASource.Id,
            seeded.UserACategory.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
        var account = await dbContext.Accounts
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == seeded.UserASource.Id);
        var goal = await dbContext.Goals
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == seeded.UserAGoal.Id);
        Assert.Equal(-65m, account.CurrentBalance);
        Assert.Equal(GoalStatus.Completed, goal.Status);
        Assert.Equal(100m, goal.CurrentAmount);
        Assert.Single(await dbContext.Transactions.AsNoTracking().ToListAsync());
    }

    [Theory]
    [InlineData(PurchaseFailurePoint.AfterSaveChanges)]
    [InlineData(PurchaseFailurePoint.BeforeCommit)]
    public async Task Purchase_rolls_back_all_changes_when_save_or_commit_fails(
        PurchaseFailurePoint failurePoint)
    {
        using var factory = new PurchaseTestFactory(failurePoint);
        var seeded = await factory.SeedAsync();
        using var client = factory.CreateAuthenticatedClient(seeded.UserAId);

        var response = await PostPurchaseAsync(
            client,
            seeded.UserAGoal.Id,
            seeded.UserASource.Id,
            seeded.UserACategory.Id);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        await AssertNoPurchaseChangesAsync(factory, seeded);
    }

    [Fact]
    public async Task Goal_purchase_transaction_cannot_be_deleted()
    {
        using var factory = new PurchaseTestFactory();
        var seeded = await factory.SeedAsync();
        using var client = factory.CreateAuthenticatedClient(seeded.UserAId);

        var purchaseResponse = await PostPurchaseAsync(
            client,
            seeded.UserAGoal.Id,
            seeded.UserASource.Id,
            seeded.UserACategory.Id);
        Assert.Equal(HttpStatusCode.OK, purchaseResponse.StatusCode);

        await using var findScope = factory.Services.CreateAsyncScope();
        var findDbContext = findScope.ServiceProvider
            .GetRequiredService<LifeDashboardDbContext>();
        var transaction = await findDbContext.Transactions
            .AsNoTracking()
            .SingleAsync(entity => entity.GoalId == seeded.UserAGoal.Id);
        var goalBeforeDelete = await findDbContext.Goals
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == seeded.UserAGoal.Id);

        var deleteResponse = await client.DeleteAsync($"/api/Transaction/{transaction.Id}");

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDbContext = verifyScope.ServiceProvider
            .GetRequiredService<LifeDashboardDbContext>();
        var persistedTransaction = await verifyDbContext.Transactions
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == transaction.Id);
        var account = await verifyDbContext.Accounts
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == seeded.UserASource.Id);
        var goal = await verifyDbContext.Goals
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == seeded.UserAGoal.Id);

        Assert.Equal(seeded.UserAGoal.Id, persistedTransaction.GoalId);
        Assert.Equal(25m, account.CurrentBalance);
        Assert.Equal(seeded.UserAGoal.CurrentAmount, goal.CurrentAmount);
        Assert.Equal(GoalStatus.Completed, goal.Status);
        Assert.Equal(goalBeforeDelete.CompletedDate, goal.CompletedDate);
    }

    [Fact]
    public async Task Goal_deposit_transfer_cannot_be_deleted()
    {
        using var factory = new PurchaseTestFactory();
        var seeded = await factory.SeedAsync(
            userAGoalType: GoalType.Saving,
            userAGoalCurrentAmount: 0m);
        using var client = factory.CreateAuthenticatedClient(seeded.UserAId);

        var depositResponse = await client.PostAsJsonAsync(
            $"/api/goals/{seeded.UserAGoal.Id}/deposit",
            new GoalDepositRequest
            {
                Amount = 25m,
                FromAccountId = seeded.UserASource.Id,
                ToAccountId = seeded.UserATarget.Id,
                Note = "Deposit delete regression"
            });
        Assert.Equal(HttpStatusCode.OK, depositResponse.StatusCode);

        await using var findScope = factory.Services.CreateAsyncScope();
        var findDbContext = findScope.ServiceProvider
            .GetRequiredService<LifeDashboardDbContext>();
        var transaction = await findDbContext.Transactions
            .AsNoTracking()
            .SingleAsync(entity => entity.GoalId == seeded.UserAGoal.Id);

        var deleteResponse = await client.DeleteAsync($"/api/Transaction/{transaction.Id}");

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDbContext = verifyScope.ServiceProvider
            .GetRequiredService<LifeDashboardDbContext>();
        var persistedTransaction = await verifyDbContext.Transactions
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == transaction.Id);
        var accounts = await verifyDbContext.Accounts
            .AsNoTracking()
            .ToDictionaryAsync(account => account.Id);
        var goal = await verifyDbContext.Goals
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == seeded.UserAGoal.Id);

        Assert.Equal(TransactionType.Transfer, persistedTransaction.Type);
        Assert.Equal(seeded.UserAGoal.Id, persistedTransaction.GoalId);
        Assert.Equal(seeded.UserASource.Id, persistedTransaction.AccountId);
        Assert.Equal(seeded.UserATarget.Id, persistedTransaction.ToAccountId);
        Assert.Equal(75m, accounts[seeded.UserASource.Id].CurrentBalance);
        Assert.Equal(125m, accounts[seeded.UserATarget.Id].CurrentBalance);
        Assert.Equal(25m, goal.CurrentAmount);
        Assert.Equal(GoalStatus.Active, goal.Status);
        Assert.Null(goal.CompletedDate);
    }

    [Fact]
    public async Task General_expense_can_be_deleted_and_refunds_account_balance()
    {
        using var factory = new PurchaseTestFactory();
        var seeded = await factory.SeedAsync();
        Guid transactionId;

        await using (var seedScope = factory.Services.CreateAsyncScope())
        {
            var dbContext = seedScope.ServiceProvider
                .GetRequiredService<LifeDashboardDbContext>();
            var account = await dbContext.Accounts
                .SingleAsync(entity => entity.Id == seeded.UserASource.Id);
            var category = await dbContext.Categories
                .SingleAsync(entity => entity.Id == seeded.UserACategory.Id);
            account.CurrentBalance = 75m;

            var transaction = new TransactionEntity(
                seeded.UserAId,
                account.Id,
                25m,
                TransactionType.Expense,
                "General expense delete regression",
                new DateOnly(2026, 11, 4))
            {
                CategoryId = category.Id,
                CategoryEntity = category
            };
            transactionId = transaction.Id;
            dbContext.Transactions.Add(transaction);
            await dbContext.SaveChangesAsync();
        }

        using var client = factory.CreateAuthenticatedClient(seeded.UserAId);
        var response = await client.DeleteAsync($"/api/Transaction/{transactionId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDbContext = verifyScope.ServiceProvider
            .GetRequiredService<LifeDashboardDbContext>();
        var accountAfterDelete = await verifyDbContext.Accounts
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == seeded.UserASource.Id);
        var goal = await verifyDbContext.Goals
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == seeded.UserAGoal.Id);

        Assert.Equal(100m, accountAfterDelete.CurrentBalance);
        Assert.False(await verifyDbContext.Transactions
            .AnyAsync(entity => entity.Id == transactionId));
        Assert.Equal(seeded.UserAGoal.CurrentAmount, goal.CurrentAmount);
        Assert.Equal(seeded.UserAGoal.Status, goal.Status);
        Assert.Equal(seeded.UserAGoal.CompletedDate, goal.CompletedDate);
    }

    private static Task<HttpResponseMessage> PostPurchaseAsync(
        HttpClient client,
        int goalId,
        Guid accountId,
        Guid categoryId,
        decimal actualAmount = 75m)
    {
        return client.PostAsJsonAsync(
            $"/api/goals/{goalId}/purchase",
            new GoalPurchaseRequest
            {
                ActualAmount = actualAmount,
                AccountId = accountId,
                CategoryId = categoryId,
                Note = "Goal purchase integration test"
            });
    }

    private static async Task AssertNoPurchaseChangesAsync(
        PurchaseTestFactory factory,
        PurchaseTestFactory.SeededData seeded)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();

        var transactions = await dbContext.Transactions
            .AsNoTracking()
            .ToListAsync();
        var goals = await dbContext.Goals
            .AsNoTracking()
            .ToListAsync();
        var userASource = await dbContext.Accounts
            .AsNoTracking()
            .SingleAsync(account => account.Id == seeded.UserASource.Id);
        var userBSource = await dbContext.Accounts
            .AsNoTracking()
            .SingleAsync(account => account.Id == seeded.UserBSource.Id);
        var userATarget = await dbContext.Accounts
            .AsNoTracking()
            .SingleAsync(account => account.Id == seeded.UserATarget.Id);
        var userAGoal = goals.Single(goal => goal.Id == seeded.UserAGoal.Id);
        var userBGoal = goals.Single(goal => goal.Id == seeded.UserBGoal.Id);

        Assert.Empty(transactions);
        Assert.Equal(seeded.UserAGoal.CurrentAmount, userAGoal.CurrentAmount);
        Assert.Equal(seeded.UserAGoal.Status, userAGoal.Status);
        Assert.Equal(seeded.UserAGoal.CompletedDate, userAGoal.CompletedDate);
        Assert.Equal(seeded.UserBGoal.CurrentAmount, userBGoal.CurrentAmount);
        Assert.Equal(seeded.UserBGoal.Status, userBGoal.Status);
        Assert.Equal(seeded.UserBGoal.CompletedDate, userBGoal.CompletedDate);
        Assert.Equal(seeded.UserASource.CurrentBalance, userASource.CurrentBalance);
        Assert.Equal(seeded.UserBSource.CurrentBalance, userBSource.CurrentBalance);
        Assert.Equal(seeded.UserATarget.CurrentBalance, userATarget.CurrentBalance);
    }

    private sealed class PurchaseTestFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");
        private readonly PurchaseFailurePoint _failurePoint;

        public PurchaseTestFactory(
            PurchaseFailurePoint failurePoint = PurchaseFailurePoint.None)
        {
            _failurePoint = failurePoint;
            _connection.Open();
        }

        public HttpClient CreateAuthenticatedClient(Guid userId)
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    CustomWebApplicationFactory.CreateJwt(userId));
            return client;
        }

        public async Task<SeededData> SeedAsync(
            decimal userASourceBalance = 100m,
            bool userASourceAllowNegativeBalance = false,
            GoalType userAGoalType = GoalType.Purchase,
            decimal userAGoalCurrentAmount = 100m,
            bool completeUserAGoal = false,
            BucketType userACategoryBucket = BucketType.Entertainment)
        {
            await using (var initializeScope = Services.CreateAsyncScope())
            {
                var dbContext = initializeScope.ServiceProvider
                    .GetRequiredService<LifeDashboardDbContext>();
                await dbContext.Database.EnsureCreatedAsync();
            }

            var userA = new User("PurchaseUserA", "test-hash");
            var userB = new User("PurchaseUserB", "test-hash");
            var userAGoal = CreateGoal(
                userA.Id,
                "User A goal",
                userAGoalType,
                userAGoalCurrentAmount,
                completeUserAGoal);
            var userBGoal = CreateGoal(
                userB.Id,
                "User B goal",
                GoalType.Purchase,
                100m);
            var userASource = CreateAccount(
                userA.Id,
                "User A source",
                userASourceBalance,
                userASourceAllowNegativeBalance);
            var userATarget = CreateAccount(userA.Id, "User A target", 100m);
            var userBSource = CreateAccount(userB.Id, "User B source", 100m);
            var userACategory = new Category(
                userA.Id,
                "Entertainment",
                "fun",
                userACategoryBucket);
            var userBCategory = new Category(
                userB.Id,
                "Entertainment",
                "fun",
                BucketType.Entertainment);

            await using (var scope = Services.CreateAsyncScope())
            {
                var dbContext = scope.ServiceProvider
                    .GetRequiredService<LifeDashboardDbContext>();
                dbContext.Users.AddRange(userA, userB);
                dbContext.Goals.AddRange(userAGoal, userBGoal);
                dbContext.Accounts.AddRange(userASource, userATarget, userBSource);
                dbContext.Categories.AddRange(userACategory, userBCategory);
                await dbContext.SaveChangesAsync();
            }

            return new SeededData(
                userA.Id,
                userAGoal,
                userBGoal,
                userASource,
                userATarget,
                userBSource,
                userACategory,
                userBCategory);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JwtSettings:SecretKey"] = "LifeDashboard.Tests.Jwt.Secret.Key.2026",
                    ["JwtSettings:Issuer"] = "LifeDashboard.Tests",
                    ["JwtSettings:Audience"] = "LifeDashboard.Tests"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<LifeDashboardDbContext>();
                services.RemoveAll<DbContextOptions<LifeDashboardDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<LifeDashboardDbContext>>();
                services.AddDbContext<LifeDashboardDbContext>(options =>
                    options.UseSqlite(_connection));

                services.RemoveAll<IUnitOfWork>();
                services.AddScoped<IUnitOfWork>(serviceProvider =>
                    new FaultInjectingUnitOfWork(
                        new UnitOfWork(
                            serviceProvider.GetRequiredService<LifeDashboardDbContext>()),
                        _failurePoint));
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _connection.Dispose();
            }
        }

        private static GoalEntity CreateGoal(
            Guid userId,
            string name,
            GoalType goalType,
            decimal currentAmount,
            bool complete = false)
        {
            var goal = new GoalEntity(
                userId,
                name,
                100m,
                GoalTier.Tier1_Safety,
                goalType,
                string.Empty);
            if (currentAmount > 0)
            {
                goal.ApplyDeposit(currentAmount);
            }

            if (complete && goal.Status == GoalStatus.Active)
            {
                goal.CompletePurchase();
            }

            return goal;
        }

        private static Account CreateAccount(
            Guid userId,
            string name,
            decimal balance,
            bool allowNegativeBalance = false)
        {
            return new Account(
                userId,
                name,
                AccountType.Cashflow,
                balance,
                allowNegativeBalance: allowNegativeBalance)
            {
                CurrentBalance = balance
            };
        }

        public sealed record SeededData(
            Guid UserAId,
            GoalEntity UserAGoal,
            GoalEntity UserBGoal,
            Account UserASource,
            Account UserATarget,
            Account UserBSource,
            Category UserACategory,
            Category UserBCategory);
    }

    private sealed class FaultInjectingUnitOfWork : IUnitOfWork
    {
        private readonly IUnitOfWork _inner;
        private readonly PurchaseFailurePoint _failurePoint;

        public FaultInjectingUnitOfWork(
            IUnitOfWork inner,
            PurchaseFailurePoint failurePoint)
        {
            _inner = inner;
            _failurePoint = failurePoint;
        }

        public Task BeginTransactionAsync() => _inner.BeginTransactionAsync();

        public async Task CommitAsync()
        {
            if (_failurePoint == PurchaseFailurePoint.BeforeCommit)
            {
                throw new IOException("Injected commit failure.");
            }

            await _inner.CommitAsync();
        }

        public Task RollbackAsync() => _inner.RollbackAsync();

        public async Task SaveChangesAsync()
        {
            await _inner.SaveChangesAsync();
            if (_failurePoint == PurchaseFailurePoint.AfterSaveChanges)
            {
                throw new DbUpdateException("Injected SaveChanges failure.");
            }
        }
    }

    public enum PurchaseFailurePoint
    {
        None,
        AfterSaveChanges,
        BeforeCommit
    }
}
