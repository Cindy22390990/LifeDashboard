using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Domain.Enums;
using LifeDashboard.Domain.ValueObjects;
using LifeDashboard.Infrastructure.Data;
using LifeDashboard.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TransactionEntity = LifeDashboard.Domain.Entities.Transaction;

namespace LifeDashboard.Tests.BudgetRadar;

public class BudgetRadarApiTests
{
    [Fact]
    public async Task GetRadar_WithoutJwt_ReturnsUnauthorized()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/budget/radar?currentDate=2026-11-02");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetRadar_WithoutCurrentDate_ReturnsBadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                CustomWebApplicationFactory.CreateJwt(Guid.NewGuid()));

        var response = await client.GetAsync("/api/budget/radar");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetRadar_WithPeriodData_ReturnsCalculatedRadarState()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        await factory.InitializeDatabaseAsync();
        var user = await SeedNormalBudgetDataAsync(factory);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                CustomWebApplicationFactory.CreateJwt(user.Id));

        var response = await client.GetAsync(
            "/api/budget/radar?currentDate=2026-11-02");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RadarStateDto>();
        result.Should().NotBeNull();
        result!.TotalLivingBudget.Should().Be(11_000m);
        result.RemainingDays.Should().Be(3);
        result.DailySafeLimit.Should().Be(2_666.67m);
        result.TodaySpent.Should().Be(600m);
        result.TodayRemaining.Should().Be(2_066.67m);
        result.UncategorizedExpenseAmount.Should().Be(250m);
    }

    [Fact]
    public async Task GetRadar_UsesOnlyAuthenticatedUsersData()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        await factory.InitializeDatabaseAsync();
        var userA = new User("UserA", "test-hash");
        var userB = new User("UserB", "test-hash");
        var accountB = new Account(userB.Id, "User B account", AccountType.Cashflow, 0m);
        var livingCategoryB = new Category(
            userB.Id,
            "User B living",
            "home",
            BucketType.Living);
        var transactionB = new TransactionEntity(
            userB.Id,
            accountB.Id,
            9_000m,
            TransactionType.Income,
            "Other user income",
            new DateOnly(2026, 10, 10));
        var expenseB = new TransactionEntity(
            userB.Id,
            accountB.Id,
            2_000m,
            TransactionType.Expense,
            "Other user expense",
            new DateOnly(2026, 11, 2))
        {
            CategoryId = livingCategoryB.Id,
            CategoryEntity = livingCategoryB
        };

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            dbContext.Users.AddRange(userA, userB);
            dbContext.SystemSettings.Add(
                CreateSetting(userA.Id, payday: 5));
            dbContext.SystemSettings.Add(
                CreateSetting(userB.Id, payday: 5));
            dbContext.Accounts.Add(accountB);
            dbContext.Categories.Add(livingCategoryB);
            dbContext.Transactions.AddRange(transactionB, expenseB);
            await dbContext.SaveChangesAsync();
        }

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                CustomWebApplicationFactory.CreateJwt(userA.Id));

        var response = await client.GetAsync(
            "/api/budget/radar?currentDate=2026-11-02");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RadarStateDto>();
        result.Should().NotBeNull();
        result!.TotalLivingBudget.Should().Be(0m);
        result.RemainingDays.Should().Be(3);
        result.DailySafeLimit.Should().Be(0m);
        result.TodaySpent.Should().Be(0m);
        result.TodayRemaining.Should().Be(0m);
        result.UncategorizedExpenseAmount.Should().Be(0m);
    }

    [Fact]
    public async Task GetRadar_WithoutSystemSetting_ReturnsNotFound()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        await factory.InitializeDatabaseAsync();
        var user = new User("NoSettingUser", "test-hash");
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
        }

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                CustomWebApplicationFactory.CreateJwt(user.Id));

        var response = await client.GetAsync(
            "/api/budget/radar?currentDate=2026-11-02");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<User> SeedNormalBudgetDataAsync(
        CustomWebApplicationFactory factory)
    {
        var user = new User("TestUser", "test-hash");
        var sourceAccount = new Account(user.Id, "Source", AccountType.Cashflow, 0m);
        var targetAccount = new Account(user.Id, "Target", AccountType.Cashflow, 0m);
        var livingCategory = new Category(
            user.Id,
            "Living",
            "home",
            BucketType.Living);
        var responsibilityCategory = new Category(
            user.Id,
            "Responsibility",
            "bill",
            BucketType.Responsibility);

        var transactions = new[]
        {
            new TransactionEntity(
                user.Id,
                sourceAccount.Id,
                12_000m,
                TransactionType.Income,
                "Period income",
                new DateOnly(2026, 10, 10)),
            CreateExpense(
                user.Id,
                sourceAccount.Id,
                2_400m,
                new DateOnly(2026, 10, 20),
                livingCategory),
            CreateExpense(
                user.Id,
                sourceAccount.Id,
                600m,
                new DateOnly(2026, 11, 2),
                livingCategory),
            CreateExpense(
                user.Id,
                sourceAccount.Id,
                800m,
                new DateOnly(2026, 11, 2),
                responsibilityCategory),
            new TransactionEntity(
                user.Id,
                sourceAccount.Id,
                250m,
                TransactionType.Expense,
                "Uncategorized",
                new DateOnly(2026, 11, 2)),
            new TransactionEntity(
                user.Id,
                sourceAccount.Id,
                400m,
                TransactionType.Transfer,
                "Transfer",
                new DateOnly(2026, 11, 2),
                targetAccount.Id)
        };

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            dbContext.Users.Add(user);
            dbContext.SystemSettings.Add(CreateSetting(user.Id, payday: 5));
            dbContext.Accounts.AddRange(sourceAccount, targetAccount);
            dbContext.Categories.AddRange(livingCategory, responsibilityCategory);
            dbContext.FixedCommitments.Add(
                TestDataFactory.CreateFixedCommitment(
                    1_000m,
                    new DateOnly(2026, 9, 1),
                    userId: user.Id));
            dbContext.Transactions.AddRange(transactions);
            await dbContext.SaveChangesAsync();
        }

        return user;
    }

    private static TransactionEntity CreateExpense(
        Guid userId,
        Guid accountId,
        decimal amount,
        DateOnly transactionDate,
        Category category)
    {
        return new TransactionEntity(
            userId,
            accountId,
            amount,
            TransactionType.Expense,
            category.Name,
            transactionDate)
        {
            CategoryId = category.Id,
            CategoryEntity = category
        };
    }

    private static SystemSetting CreateSetting(Guid userId, int payday)
    {
        return new SystemSetting(
            userId,
            "Test",
            payday,
            new AllocationRatio(100, 0, 0, 0));
    }
}
