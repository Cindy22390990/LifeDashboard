using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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

public class BudgetRadarSoftDeleteRegressionTests
{
    [Fact]
    public async Task Deleted_expense_does_not_affect_spending_calculation()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        var user = new User($"BudgetRadarSoftDelete-{Guid.NewGuid():N}", "test-hash");
        var account = new Account(user.Id, "Radar account", AccountType.Cashflow, 10_000m);
        var category = new Category(user.Id, "Food", "food", BucketType.Living);
        var activeTransaction = CreateExpense(user.Id, account.Id, category, 3_000m);
        var deletedTransaction = CreateExpense(user.Id, account.Id, category, 5_000m);

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

            deletedTransaction.MarkAsDeleted("Budget Radar regression test");
            await dbContext.SaveChangesAsync();
        }

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CustomWebApplicationFactory.CreateJwt(user.Id));

        var response = await client.GetAsync(
            "/api/budget/radar?currentDate=2026-11-02");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var radar = await response.Content.ReadFromJsonAsync<RadarStateDto>();
        Assert.NotNull(radar);
        Assert.Equal(3_000m, radar.TodaySpent);
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
