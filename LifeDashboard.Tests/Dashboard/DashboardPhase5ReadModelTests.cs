using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Domain.Enums;
using LifeDashboard.Domain.ValueObjects;
using LifeDashboard.Infrastructure.Data;
using LifeDashboard.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TransactionEntity = LifeDashboard.Domain.Entities.Transaction;

namespace LifeDashboard.Tests.Dashboard;

public class DashboardPhase5ReadModelTests
{
    [Fact]
    public async Task Summary_returns_approved_fields_with_user_owned_accounts_and_filtered_transactions()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        var user = new User($"DashboardPhase5-{Guid.NewGuid():N}", "test-hash");
        var otherUser = new User($"DashboardPhase5Other-{Guid.NewGuid():N}", "test-hash");
        var userAccount = new Account(
            user.Id,
            "User account",
            AccountType.Cashflow,
            10_000m);
        var systemAccount = new Account(
            user.Id,
            "System account",
            AccountType.Cashflow,
            4_000m,
            isSystemAccount: true);
        var deletedAccount = new Account(
            user.Id,
            "Deleted account",
            AccountType.Cashflow,
            7_000m);
        deletedAccount.MarkAsDeleted();
        var otherUserAccount = new Account(
            otherUser.Id,
            "Other user account",
            AccountType.Cashflow,
            9_000m);
        var category = new Category(user.Id, "Food", "food", BucketType.Living);

        var activeTransactions = new[]
        {
            CreateTransaction(user.Id, userAccount.Id, TransactionType.Income, 1_000m, new DateOnly(2026, 11, 1)),
            CreateTransaction(user.Id, userAccount.Id, TransactionType.Expense, 10m, new DateOnly(2026, 11, 2), category),
            CreateTransaction(user.Id, userAccount.Id, TransactionType.Expense, 20m, new DateOnly(2026, 11, 3), category),
            CreateTransaction(user.Id, userAccount.Id, TransactionType.Expense, 30m, new DateOnly(2026, 11, 4), category),
            CreateTransaction(user.Id, userAccount.Id, TransactionType.Expense, 40m, new DateOnly(2026, 11, 5), category),
            CreateTransaction(user.Id, userAccount.Id, TransactionType.Expense, 50m, new DateOnly(2026, 11, 6), category),
            CreateTransaction(user.Id, userAccount.Id, TransactionType.Income, 5_000m, new DateOnly(2026, 12, 1))
        };
        var deletedExpense = CreateTransaction(
            user.Id,
            userAccount.Id,
            TransactionType.Expense,
            888m,
            new DateOnly(2026, 11, 30),
            category);
        var deletedIncome = CreateTransaction(
            user.Id,
            userAccount.Id,
            TransactionType.Income,
            9_999m,
            new DateOnly(2026, 11, 29));
        var otherUserExpense = CreateTransaction(
            otherUser.Id,
            otherUserAccount.Id,
            TransactionType.Expense,
            600m,
            new DateOnly(2026, 11, 8),
            new Category(otherUser.Id, "Other food", "other-food", BucketType.Living));

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            dbContext.Users.AddRange(user, otherUser);
            dbContext.Accounts.AddRange(
                userAccount,
                systemAccount,
                deletedAccount,
                otherUserAccount);
            dbContext.Categories.AddRange(category, otherUserExpense.CategoryEntity!);
            dbContext.SystemSettings.Add(new SystemSetting(
                user.Id,
                "Test",
                1,
                new AllocationRatio(100, 0, 0, 0)));
            dbContext.Transactions.AddRange(
                activeTransactions
                    .Append(deletedExpense)
                    .Append(deletedIncome)
                    .Append(otherUserExpense));
            await dbContext.SaveChangesAsync();

            deletedExpense.MarkAsDeleted("Dashboard Phase 5 read-model test");
            deletedIncome.MarkAsDeleted("Dashboard Phase 5 read-model test");
            await dbContext.SaveChangesAsync();
        }

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CustomWebApplicationFactory.CreateJwt(user.Id));

        var response = await client.GetAsync(
            "/api/dashboard/summary?currentDate=2026-11-10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal(3, root.EnumerateObject().Count());
        Assert.Equal(10_000m, root.GetProperty("totalAssets").GetDecimal());
        Assert.Equal(1_000m, root.GetProperty("monthlyIncome").GetDecimal());
        Assert.Equal(150m, root.GetProperty("monthlyExpense").GetDecimal());
        Assert.False(root.TryGetProperty("balance", out _));
        Assert.False(root.TryGetProperty("netWorth", out _));
        Assert.False(root.TryGetProperty("recentActivity", out _));
    }

    private static TransactionEntity CreateTransaction(
        Guid userId,
        Guid accountId,
        TransactionType type,
        decimal amount,
        DateOnly date,
        Category? category = null)
    {
        return new TransactionEntity(
            userId,
            accountId,
            amount,
            type,
            category?.Name ?? "Salary",
            date)
        {
            CategoryId = category?.Id,
            CategoryEntity = category
        };
    }
}
