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

public class DashboardSoftDeleteRegressionTests
{
    [Fact]
    public async Task Deleted_transaction_does_not_affect_dashboard_summary()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        var user = new User($"DashboardSoftDelete-{Guid.NewGuid():N}", "test-hash");
        var account = new Account(
            user.Id,
            "Dashboard account",
            AccountType.Cashflow,
            10_000m);
        var category = new Category(user.Id, "Food", "food", BucketType.Living);
        var activeTransaction = CreateExpense(user.Id, account.Id, category, 1_000m);
        var deletedTransaction = CreateExpense(user.Id, account.Id, category, 2_000m);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            dbContext.Users.Add(user);
            dbContext.Accounts.Add(account);
            dbContext.Categories.Add(category);
            dbContext.SystemSettings.Add(new SystemSetting(
                user.Id,
                "Test",
                1,
                new AllocationRatio(100, 0, 0, 0)));
            dbContext.Transactions.AddRange(activeTransaction, deletedTransaction);
            await dbContext.SaveChangesAsync();

            deletedTransaction.MarkAsDeleted("Dashboard read-model regression test");
            await dbContext.SaveChangesAsync();
        }

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CustomWebApplicationFactory.CreateJwt(user.Id));

        var response = await client.GetAsync(
            "/api/dashboard/summary?currentDate=2026-11-02");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal(3, root.EnumerateObject().Count());
        Assert.Equal(10_000m, root.GetProperty("totalAssets").GetDecimal());
        Assert.Equal(0m, root.GetProperty("monthlyIncome").GetDecimal());
        Assert.Equal(1_000m, root.GetProperty("monthlyExpense").GetDecimal());
    }

    private static TransactionEntity CreateExpense(
        Guid userId,
        Guid accountId,
        Category category,
        decimal amount)
    {
        return new TransactionEntity(
            userId,
            accountId,
            amount,
            TransactionType.Expense,
            category.Name,
            new DateOnly(2026, 11, 2))
        {
            CategoryId = category.Id,
            CategoryEntity = category
        };
    }
}
