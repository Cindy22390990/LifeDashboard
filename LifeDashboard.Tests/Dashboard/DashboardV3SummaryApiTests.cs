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

public sealed class DashboardV3SummaryApiTests
{
    [Fact]
    public async Task Authorized_request_returns_v3_dashboard_summary_contract()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var user = await SeedDashboardDataAsync(
            factory,
            "V3AuthorizedUser",
            "Authorized account",
            1_250_000m,
            125m,
            "Authorized grocery",
            today);

        using var client = CreateAuthenticatedClient(factory, user.Id);
        using var response = await client.GetAsync("/api/dashboard/v3/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>();
        Assert.NotNull(result);
        Assert.Equal(1_250_000m, result.TotalAssets);
        Assert.Equal(125m, result.TodayExpense);
        Assert.Equal(today, result.BusinessDate);
        var account = Assert.Single(result.Accounts);
        Assert.Equal("Authorized account", account.Name);
        Assert.Equal(AccountType.Cashflow, account.Type);
        Assert.Equal("Cashflow", account.AccountType);
        Assert.Equal(1_250_000m, account.Balance);

        var transaction = Assert.Single(result.RecentTransactions);
        Assert.Equal(125m, transaction.Amount);
        Assert.Equal(TransactionType.Expense, transaction.Type);
        Assert.Equal("Expense", transaction.TransactionType);
        Assert.Equal("Authorized grocery", transaction.Note);
        Assert.Equal(today, transaction.TransactionDate);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var accountJson = json.RootElement.GetProperty("accounts")[0];
        Assert.Equal(JsonValueKind.Number, accountJson.GetProperty("type").ValueKind);
        Assert.Equal((int)AccountType.Cashflow, accountJson.GetProperty("type").GetInt32());
        Assert.Equal(JsonValueKind.String, accountJson.GetProperty("accountType").ValueKind);
        Assert.Equal("Cashflow", accountJson.GetProperty("accountType").GetString());

        var transactionJson = json.RootElement.GetProperty("recentTransactions")[0];
        Assert.Equal(JsonValueKind.Number, transactionJson.GetProperty("type").ValueKind);
        Assert.Equal((int)TransactionType.Expense, transactionJson.GetProperty("type").GetInt32());
        Assert.Equal(JsonValueKind.String, transactionJson.GetProperty("transactionType").ValueKind);
        Assert.Equal("Expense", transactionJson.GetProperty("transactionType").GetString());
    }

    [Fact]
    public async Task Unauthorized_request_returns_unauthorized()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/dashboard/v3/summary");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Authorized_request_reads_only_the_authenticated_users_dashboard_data()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var userA = await SeedDashboardDataAsync(
            factory,
            "V3UserA",
            "User A account",
            100_000m,
            300m,
            "User A expense",
            today);

        await SeedDashboardDataAsync(
            factory,
            "V3UserB",
            "User B account",
            900_000m,
            7_000m,
            "User B expense",
            today);

        using var client = CreateAuthenticatedClient(factory, userA.Id);
        using var response = await client.GetAsync("/api/dashboard/v3/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>();
        Assert.NotNull(result);
        Assert.Equal(100_000m, result.TotalAssets);
        Assert.Equal(300m, result.TodayExpense);
        Assert.Equal(today, result.BusinessDate);
        var account = Assert.Single(result.Accounts);
        Assert.Equal("User A account", account.Name);
        Assert.Equal(100_000m, account.Balance);

        var transaction = Assert.Single(result.RecentTransactions);
        Assert.Equal("User A expense", transaction.Note);
        Assert.Equal("User A account", transaction.AccountName);
        Assert.Equal(300m, transaction.Amount);
        Assert.Equal(TransactionType.Expense, transaction.Type);
    }

    [Fact]
    public async Task System_account_is_excluded_from_accounts_and_total_assets()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var user = new User("V3SystemAccountUser", "test-hash");
        var normalAccount = new Account(
            user.Id,
            "Normal account",
            AccountType.Cashflow,
            50_000m);
        var systemAccount = new Account(
            user.Id,
            "System account",
            AccountType.Saving,
            75_000m,
            isSystemAccount: true);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            dbContext.Users.Add(user);
            dbContext.Accounts.AddRange(normalAccount, systemAccount);
            await dbContext.SaveChangesAsync();
        }

        using var client = CreateAuthenticatedClient(factory, user.Id);
        using var response = await client.GetAsync("/api/dashboard/v3/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>();
        Assert.NotNull(result);
        Assert.Equal(50_000m, result.TotalAssets);
        Assert.Equal(today, result.BusinessDate);
        var account = Assert.Single(result.Accounts);
        Assert.Equal(normalAccount.Id, account.Id);
        Assert.Equal("Normal account", account.Name);
        Assert.DoesNotContain(result.Accounts, item => item.Id == systemAccount.Id);
    }

    private static async Task<User> SeedDashboardDataAsync(
        CustomWebApplicationFactory factory,
        string username,
        string accountName,
        decimal accountBalance,
        decimal expenseAmount,
        string expenseNote,
        DateOnly transactionDate)
    {
        var user = new User(username, "test-hash");
        var account = new Account(
            user.Id,
            accountName,
            AccountType.Cashflow,
            accountBalance);
        account.CurrentBalance = accountBalance;
        var category = new Category(
            user.Id,
            "Test category",
            "test",
            BucketType.Living);
        var transaction = new TransactionEntity(
            user.Id,
            account.Id,
            expenseAmount,
            TransactionType.Expense,
            expenseNote,
            transactionDate)
        {
            CategoryId = category.Id,
            CategoryEntity = category
        };

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
        dbContext.Users.Add(user);
        dbContext.Accounts.Add(account);
        dbContext.Categories.Add(category);
        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync();

        return user;
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
