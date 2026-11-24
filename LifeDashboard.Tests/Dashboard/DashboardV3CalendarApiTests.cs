using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LifeDashboard.Application.Features.Dashboard.Dtos;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Domain.Enums;
using LifeDashboard.Infrastructure.Data;
using LifeDashboard.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using TransactionEntity = LifeDashboard.Domain.Entities.Transaction;

namespace LifeDashboard.Tests.Dashboard;

public sealed class DashboardV3CalendarApiTests
{
    [Fact]
    public async Task Day_detail_aggregates_transactions_and_returns_string_types_in_created_order()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        var date = new DateOnly(2026, 11, 24);
        var user = new User("CalendarDayUser", "test-hash");
        var account = new Account(user.Id, "Main account", AccountType.Cashflow, 0m);
        var expense = new TransactionEntity(
            user.Id,
            account.Id,
            25m,
            TransactionType.Expense,
            "Expense",
            date);
        await Task.Delay(2);
        var income = new TransactionEntity(
            user.Id,
            account.Id,
            200m,
            TransactionType.Income,
            "Income",
            date);
        await Task.Delay(2);
        var transfer = new TransactionEntity(
            user.Id,
            account.Id,
            50m,
            TransactionType.Transfer,
            "Transfer",
            date);
        var deleted = new TransactionEntity(
            user.Id,
            account.Id,
            900m,
            TransactionType.Expense,
            "Deleted",
            date);
        deleted.MarkAsDeleted("Excluded from calendar read.");

        await SeedAsync(factory, user, account, expense, income, transfer, deleted);

        using var client = CreateAuthenticatedClient(factory, user.Id);
        using var response = await client.GetAsync(
            "/api/dashboard/v3/calendar/day?date=2026-11-24");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<DashboardDayDetailDto>();
        Assert.NotNull(result);
        Assert.Equal(date, result.Date);
        Assert.Equal(200m, result.TotalIncome);
        Assert.Equal(25m, result.TotalExpense);
        Assert.Equal(3, result.Transactions.Count);

        var expectedIds = new[] { expense, income, transfer }
            .OrderByDescending(transaction => transaction.CreatedAtUtc)
            .Select(transaction => transaction.Id)
            .ToArray();
        Assert.Equal(expectedIds, result.Transactions.Select(transaction => transaction.Id));

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var firstTransaction = json.RootElement.GetProperty("transactions")[0];
        Assert.True(firstTransaction.GetProperty("type").ValueKind == JsonValueKind.String);
        Assert.False(firstTransaction.TryGetProperty("transactionType", out _));
        Assert.Equal(
            new[] { "Expense", "Income", "Transfer" },
            result.Transactions
                .Select(transaction => transaction.Type)
                .OrderBy(type => type));
    }

    [Fact]
    public async Task Empty_day_returns_zero_totals_and_an_empty_array()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        var user = new User("CalendarEmptyDayUser", "test-hash");
        var account = new Account(user.Id, "Main account", AccountType.Cashflow, 0m);
        await SeedAsync(factory, user, account);

        using var client = CreateAuthenticatedClient(factory, user.Id);
        using var response = await client.GetAsync(
            "/api/dashboard/v3/calendar/day?date=2026-11-24");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<DashboardDayDetailDto>();
        Assert.NotNull(result);
        Assert.Equal(0m, result.TotalIncome);
        Assert.Equal(0m, result.TotalExpense);
        Assert.Empty(result.Transactions);
    }

    [Fact]
    public async Task Month_summary_includes_only_active_user_transaction_dates_and_keeps_transfer_flag()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        var user = new User("CalendarMonthUser", "test-hash");
        var account = new Account(user.Id, "Main account", AccountType.Cashflow, 0m);
        var firstDay = new DateOnly(2026, 11, 2);
        var transferDay = new DateOnly(2026, 11, 19);
        var transactions = new[]
        {
            new TransactionEntity(user.Id, account.Id, 125m, TransactionType.Income, "Income", firstDay),
            new TransactionEntity(user.Id, account.Id, 20m, TransactionType.Expense, "Expense", firstDay),
            new TransactionEntity(user.Id, account.Id, 80m, TransactionType.Transfer, "Transfer", firstDay),
            new TransactionEntity(user.Id, account.Id, 65m, TransactionType.Transfer, "Transfer only", transferDay)
        };
        var deleted = new TransactionEntity(
            user.Id,
            account.Id,
            500m,
            TransactionType.Income,
            "Deleted",
            new DateOnly(2026, 11, 28));
        deleted.MarkAsDeleted("Excluded from calendar read.");

        var otherUser = new User("CalendarOtherUser", "test-hash");
        var otherAccount = new Account(otherUser.Id, "Other account", AccountType.Cashflow, 0m);
        var otherUserTransaction = new TransactionEntity(
            otherUser.Id,
            otherAccount.Id,
            700m,
            TransactionType.Income,
            "Other user",
            new DateOnly(2026, 11, 12));
        var otherMonthTransaction = new TransactionEntity(
            user.Id,
            account.Id,
            900m,
            TransactionType.Income,
            "Other month",
            new DateOnly(2026, 12, 1));

        await SeedAsync(
            factory,
            user,
            account,
            transactions.Concat([deleted, otherMonthTransaction]).ToArray());
        await SeedAsync(factory, otherUser, otherAccount, otherUserTransaction);

        using var client = CreateAuthenticatedClient(factory, user.Id);
        using var response = await client.GetAsync(
            "/api/dashboard/v3/calendar?year=2026&month=11");

        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, responseBody);
        var result = await response.Content.ReadFromJsonAsync<DashboardCalendarMonthDto>();
        Assert.NotNull(result);
        Assert.Equal(2026, result.Year);
        Assert.Equal(11, result.Month);
        Assert.Equal(2, result.Days.Count);

        var incomeExpenseDay = Assert.Single(result.Days, day => day.Date == firstDay);
        Assert.Equal(125m, incomeExpenseDay.TotalIncome);
        Assert.Equal(20m, incomeExpenseDay.TotalExpense);
        Assert.True(incomeExpenseDay.HasTransfer);

        var transferOnlyDay = Assert.Single(result.Days, day => day.Date == transferDay);
        Assert.Equal(0m, transferOnlyDay.TotalIncome);
        Assert.Equal(0m, transferOnlyDay.TotalExpense);
        Assert.True(transferOnlyDay.HasTransfer);
    }

    [Theory]
    [InlineData("/api/dashboard/v3/calendar/day?date=2026-11-24")]
    [InlineData("/api/dashboard/v3/calendar?year=2026&month=11")]
    public async Task Calendar_endpoints_require_authorization(string path)
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Day_endpoint_rejects_an_invalid_date()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = CreateAuthenticatedClient(factory, Guid.NewGuid());

        using var response = await client.GetAsync(
            "/api/dashboard/v3/calendar/day?date=test");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Month_endpoint_rejects_an_invalid_month()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = CreateAuthenticatedClient(factory, Guid.NewGuid());

        using var response = await client.GetAsync(
            "/api/dashboard/v3/calendar?year=2026&month=13");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task SeedAsync(
        CustomWebApplicationFactory factory,
        User user,
        Account account,
        params TransactionEntity[] transactions)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
        dbContext.Users.Add(user);
        dbContext.Accounts.Add(account);
        dbContext.Transactions.AddRange(transactions);
        await dbContext.SaveChangesAsync();
    }

    private static HttpClient CreateAuthenticatedClient(
        CustomWebApplicationFactory factory,
        Guid userId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CustomWebApplicationFactory.CreateJwt(userId));
        return client;
    }
}
