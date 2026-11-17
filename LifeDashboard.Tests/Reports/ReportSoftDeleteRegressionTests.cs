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
using TransactionEntity = LifeDashboard.Domain.Entities.Transaction;

namespace LifeDashboard.Tests.Reports;

public class ReportSoftDeleteRegressionTests
{
    [Fact]
    public async Task Deleted_transaction_is_excluded_from_monthly_report()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        var user = new User($"ReportSoftDelete-{Guid.NewGuid():N}", "test-hash");
        var account = new Account(user.Id, "Report account", AccountType.Cashflow, 10_000m);
        var activeTransaction = CreateExpense(user.Id, account.Id, 2_000m);
        var deletedTransaction = CreateExpense(user.Id, account.Id, 7_000m);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            dbContext.Users.Add(user);
            dbContext.Accounts.Add(account);
            dbContext.Transactions.AddRange(activeTransaction, deletedTransaction);
            await dbContext.SaveChangesAsync();

            deletedTransaction.MarkAsDeleted("Monthly report regression test");
            await dbContext.SaveChangesAsync();
        }

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CustomWebApplicationFactory.CreateJwt(user.Id));

        var response = await client.GetAsync("/api/reports/monthly?year=2026&month=11");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<MonthlyReportDto>();
        Assert.NotNull(report);
        Assert.Equal(2_000m, report.TotalExpense);
    }

    private static TransactionEntity CreateExpense(
        Guid userId,
        Guid accountId,
        decimal amount)
    {
        return new TransactionEntity(
            userId,
            accountId,
            amount,
            TransactionType.Expense,
            "Monthly report test",
            new DateOnly(2026, 11, 2));
    }
}
