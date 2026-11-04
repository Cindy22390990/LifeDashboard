using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LifeDashboard.Application.Interfaces;
using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Api;
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

namespace LifeDashboard.Tests.Goal;

public class GoalDepositSecurityAndAtomicityTests
{
    [Fact]
    public async Task Deposit_with_another_users_goal_does_not_change_any_data()
    {
        using var factory = new DepositTestFactory();
        var users = await factory.SeedAsync();
        using var client = await factory.CreateAuthenticatedClientAsync();

        var response = await PostDepositAsync(
            client,
            users.UserBGoal.Id,
            users.UserASource.Id,
            users.UserATarget.Id);

        Assert.False(response.IsSuccessStatusCode);
        await AssertSeededStateUnchangedAsync(factory);
    }

    [Fact]
    public async Task Deposit_with_another_users_source_account_does_not_change_any_data()
    {
        using var factory = new DepositTestFactory();
        var users = await factory.SeedAsync();
        using var client = await factory.CreateAuthenticatedClientAsync();

        var response = await PostDepositAsync(
            client,
            users.UserAGoal.Id,
            users.UserBSource.Id,
            users.UserATarget.Id);

        Assert.False(response.IsSuccessStatusCode);
        await AssertSeededStateUnchangedAsync(factory);
    }

    [Fact]
    public async Task Deposit_with_another_users_target_account_does_not_change_any_data()
    {
        using var factory = new DepositTestFactory();
        var users = await factory.SeedAsync();
        using var client = await factory.CreateAuthenticatedClientAsync();

        var response = await PostDepositAsync(
            client,
            users.UserAGoal.Id,
            users.UserASource.Id,
            users.UserBTarget.Id);

        Assert.False(response.IsSuccessStatusCode);
        await AssertSeededStateUnchangedAsync(factory);
    }

    [Theory]
    [InlineData(DepositFailurePoint.AfterSaveChanges)]
    [InlineData(DepositFailurePoint.BeforeCommit)]
    public async Task Deposit_rolls_back_all_changes_when_save_or_commit_fails(
        DepositFailurePoint failurePoint)
    {
        using var factory = new DepositTestFactory(failurePoint);
        var users = await factory.SeedAsync();
        using var client = await factory.CreateAuthenticatedClientAsync();

        var response = await PostDepositAsync(
            client,
            users.UserAGoal.Id,
            users.UserASource.Id,
            users.UserATarget.Id);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertSeededStateUnchangedAsync(factory);
    }

    private static Task<HttpResponseMessage> PostDepositAsync(
        HttpClient client,
        int goalId,
        Guid fromAccountId,
        Guid toAccountId)
    {
        return client.PostAsJsonAsync(
            $"/api/goals/{goalId}/deposit",
            new GoalDepositRequest
            {
                Amount = 25m,
                FromAccountId = fromAccountId,
                ToAccountId = toAccountId,
                Note = "Security and rollback test"
            });
    }

    private static async Task AssertSeededStateUnchangedAsync(
        DepositTestFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<LifeDashboardDbContext>();

        var goals = await dbContext.Goals
            .AsNoTracking()
            .OrderBy(goal => goal.Id)
            .ToListAsync();
        var accounts = await dbContext.Accounts
            .AsNoTracking()
            .OrderBy(account => account.Id)
            .ToListAsync();
        var transactions = await dbContext.Transactions
            .AsNoTracking()
            .ToListAsync();

        Assert.Equal(2, goals.Count);
        Assert.All(goals, goal => Assert.Equal(0m, goal.CurrentAmount));
        Assert.All(goals, goal => Assert.Equal(GoalStatus.Active, goal.Status));

        Assert.Equal(4, accounts.Count);
        Assert.All(accounts, account => Assert.Equal(100m, account.CurrentBalance));
        Assert.Empty(transactions);
    }

    private sealed class DepositTestFactory : WebApplicationFactory<Program>
    {
        private const string JwtSecret = "LifeDashboard.Tests.Jwt.Secret.Key.2026";
        private readonly SqliteConnection _connection = new("Data Source=:memory:");
        private readonly DepositFailurePoint _failurePoint;

        public DepositTestFactory(
            DepositFailurePoint failurePoint = DepositFailurePoint.None)
        {
            _failurePoint = failurePoint;
            _connection.Open();
        }

        public async Task<HttpClient> CreateAuthenticatedClientAsync()
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    CustomWebApplicationFactory.CreateJwt(_userAId));
            await InitializeDatabaseAsync();
            return client;
        }

        private Guid _userAId;

        public async Task<SeededUsers> SeedAsync()
        {
            await InitializeDatabaseAsync();
            var userA = new User("DepositUserA", "test-hash");
            var userB = new User("DepositUserB", "test-hash");
            _userAId = userA.Id;

            var userAGoal = CreateGoal(userA.Id, "User A goal");
            var userBGoal = CreateGoal(userB.Id, "User B goal");
            var userASource = CreateAccount(userA.Id, "User A source");
            var userATarget = CreateAccount(userA.Id, "User A target");
            var userBSource = CreateAccount(userB.Id, "User B source");
            var userBTarget = CreateAccount(userB.Id, "User B target");

            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider
                .GetRequiredService<LifeDashboardDbContext>();
            dbContext.Users.AddRange(userA, userB);
            dbContext.Goals.AddRange(userAGoal, userBGoal);
            dbContext.Accounts.AddRange(
                userASource,
                userATarget,
                userBSource,
                userBTarget);
            await dbContext.SaveChangesAsync();

            return new SeededUsers(
                userAGoal,
                userBGoal,
                userASource,
                userATarget,
                userBSource,
                userBTarget);
        }

        private async Task InitializeDatabaseAsync()
        {
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider
                .GetRequiredService<LifeDashboardDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JwtSettings:SecretKey"] = JwtSecret,
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

        private static GoalEntity CreateGoal(Guid userId, string name)
        {
            return new GoalEntity(
                userId,
                name,
                100m,
                GoalTier.Tier1_Safety,
                GoalType.Saving,
                string.Empty);
        }

        private static Account CreateAccount(Guid userId, string name)
        {
            return new Account(userId, name, AccountType.Cashflow, 100m)
            {
                CurrentBalance = 100m
            };
        }

        public sealed record SeededUsers(
            GoalEntity UserAGoal,
            GoalEntity UserBGoal,
            Account UserASource,
            Account UserATarget,
            Account UserBSource,
            Account UserBTarget);
    }

    private sealed class FaultInjectingUnitOfWork : IUnitOfWork
    {
        private readonly IUnitOfWork _inner;
        private readonly DepositFailurePoint _failurePoint;

        public FaultInjectingUnitOfWork(
            IUnitOfWork inner,
            DepositFailurePoint failurePoint)
        {
            _inner = inner;
            _failurePoint = failurePoint;
        }

        public Task BeginTransactionAsync() => _inner.BeginTransactionAsync();

        public async Task CommitAsync()
        {
            if (_failurePoint == DepositFailurePoint.BeforeCommit)
            {
                throw new InvalidOperationException("Injected commit failure.");
            }

            await _inner.CommitAsync();
        }

        public Task RollbackAsync() => _inner.RollbackAsync();

        public async Task SaveChangesAsync()
        {
            await _inner.SaveChangesAsync();
            if (_failurePoint == DepositFailurePoint.AfterSaveChanges)
            {
                throw new InvalidOperationException("Injected save failure.");
            }
        }
    }

    public enum DepositFailurePoint
    {
        None,
        AfterSaveChanges,
        BeforeCommit
    }
}
