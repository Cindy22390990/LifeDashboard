using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LifeDashboard.Application.Features.V4.Dtos;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Domain.Enums;
using LifeDashboard.Infrastructure.Data;
using LifeDashboard.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using TransactionEntity = LifeDashboard.Domain.Entities.Transaction;

namespace LifeDashboard.Tests.V4;

public sealed class DashboardSummaryApiTests
{
    private static HttpClient CreateAuthenticatedClient(
        CustomWebApplicationFactory factory,
        Guid userId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                CustomWebApplicationFactory.CreateJwt(userId));
        return client;
    }

    [Fact]
    public async Task GetSummary_ShouldReturnFrozenSchemaAndCurrentMonthTotals()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        var user = new User("V4SummaryRead", "test-hash");
        var account = new Account(
            user.Id,
            "Cashflow",
            AccountType.Cashflow,
            1_000m);
        var systemAccount = new Account(
            user.Id,
            "System",
            AccountType.Cashflow,
            5_000m,
            isSystemAccount: true);
        var deletedAccount = new Account(
            user.Id,
            "Deleted",
            AccountType.Cashflow,
            2_000m);
        deletedAccount.MarkAsDeleted();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var previousMonth = today.AddMonths(-1);

        var currentIncome = new TransactionEntity(
            user.Id,
            account.Id,
            300m,
            TransactionType.Income,
            "Salary",
            today);
        var currentExpense = new TransactionEntity(
            user.Id,
            account.Id,
            125m,
            TransactionType.Expense,
            "Lunch",
            today);
        var transfer = new TransactionEntity(
            user.Id,
            account.Id,
            50m,
            TransactionType.Transfer,
            "Transfer",
            today,
            account.Id);
        var previousIncome = new TransactionEntity(
            user.Id,
            account.Id,
            9_000m,
            TransactionType.Income,
            "Previous salary",
            previousMonth);
        var previousExpense = new TransactionEntity(
            user.Id,
            account.Id,
            8_000m,
            TransactionType.Expense,
            "Previous expense",
            previousMonth);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            db.Users.Add(user);
            db.Accounts.AddRange(account, systemAccount, deletedAccount);
            db.Transactions.AddRange(
                currentIncome,
                currentExpense,
                transfer,
                previousIncome,
                previousExpense);
            await db.SaveChangesAsync();
        }

        using var client = CreateAuthenticatedClient(factory, user.Id);
        using var response = await client.GetAsync("/api/dashboard/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal(3, root.EnumerateObject().Count());
        Assert.True(root.TryGetProperty("totalAssets", out _));
        Assert.True(root.TryGetProperty("monthlyIncome", out _));
        Assert.True(root.TryGetProperty("monthlyExpense", out _));
        Assert.False(root.TryGetProperty("balance", out _));
        Assert.False(root.TryGetProperty("netWorth", out _));
        Assert.False(root.TryGetProperty("recentActivity", out _));

        var result = JsonSerializer.Deserialize<DashboardSummaryReadDto>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(result);
        Assert.Equal(1_000m, result.TotalAssets);
        Assert.Equal(300m, result.MonthlyIncome);
        Assert.Equal(125m, result.MonthlyExpense);
    }

    [Fact]
    public async Task GetSummary_ShouldReturnZeros_WhenUserHasNoEligibleData()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        var user = new User("V4SummaryEmpty", "test-hash");
        var systemAccount = new Account(
            user.Id,
            "System",
            AccountType.Cashflow,
            10_000m,
            isSystemAccount: true);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            db.Users.Add(user);
            db.Accounts.Add(systemAccount);
            await db.SaveChangesAsync();
        }

        using var client = CreateAuthenticatedClient(factory, user.Id);
        using var response = await client.GetAsync("/api/dashboard/summary");
        var result = await response.Content
            .ReadFromJsonAsync<DashboardSummaryReadDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(0m, result.TotalAssets);
        Assert.Equal(0m, result.MonthlyIncome);
        Assert.Equal(0m, result.MonthlyExpense);
    }

    [Fact]
    public async Task GetSummary_ShouldNotLeakAnotherUsersData()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        var owner = new User("V4SummaryOwner", "test-hash");
        var otherUser = new User("V4SummaryOther", "test-hash");
        var ownerAccount = new Account(
            owner.Id,
            "Owner",
            AccountType.Cashflow,
            400m);
        var otherAccount = new Account(
            otherUser.Id,
            "Other",
            AccountType.Cashflow,
            900m);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var ownerIncome = new TransactionEntity(
            owner.Id,
            ownerAccount.Id,
            200m,
            TransactionType.Income,
            "Owner income",
            today);
        var otherIncome = new TransactionEntity(
            otherUser.Id,
            otherAccount.Id,
            700m,
            TransactionType.Income,
            "Other income",
            today);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            db.Users.AddRange(owner, otherUser);
            db.Accounts.AddRange(ownerAccount, otherAccount);
            db.Transactions.AddRange(ownerIncome, otherIncome);
            await db.SaveChangesAsync();
        }

        using var client = CreateAuthenticatedClient(factory, owner.Id);
        using var response = await client.GetAsync("/api/dashboard/summary");
        var result = await response.Content
            .ReadFromJsonAsync<DashboardSummaryReadDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(400m, result.TotalAssets);
        Assert.Equal(200m, result.MonthlyIncome);
        Assert.Equal(0m, result.MonthlyExpense);
    }

    [Fact]
    public async Task GetSummary_ShouldReturnUnauthorized_WhenNoAuthentication()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/dashboard/summary");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
