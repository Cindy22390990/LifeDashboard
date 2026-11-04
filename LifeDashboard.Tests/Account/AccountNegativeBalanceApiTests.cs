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

namespace LifeDashboard.Tests.AccountPolicy;

public class AccountNegativeBalanceApiTests
{
    [Fact]
    public async Task Create_account_defaults_allow_negative_balance_to_false_and_returns_setting()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        var user = await CreateUserAsync(factory);
        using var client = CreateAuthenticatedClient(factory, user.Id);

        var defaultResponse = await client.PostAsJsonAsync(
            "/api/Account",
            new CreateAccountDto
            {
                Name = "Default account",
                Type = AccountType.Cashflow,
                InitialBalance = 100m
            });
        var enabledResponse = await client.PostAsJsonAsync(
            "/api/Account",
            new CreateAccountDto
            {
                Name = "Overdraft account",
                Type = AccountType.Cashflow,
                InitialBalance = 100m,
                AllowNegativeBalance = true
            });

        Assert.Equal(HttpStatusCode.OK, defaultResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, enabledResponse.StatusCode);
        var defaultAccount = await defaultResponse.Content.ReadFromJsonAsync<AccountDto>();
        var enabledAccount = await enabledResponse.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(defaultAccount);
        Assert.NotNull(enabledAccount);
        Assert.False(defaultAccount.AllowNegativeBalance);
        Assert.True(enabledAccount.AllowNegativeBalance);

        using var listResponse = await client.GetAsync("/api/Account");
        var accounts = await listResponse.Content.ReadFromJsonAsync<List<AccountDto>>();
        Assert.NotNull(accounts);
        Assert.Equal(2, accounts.Count);
        Assert.Equal(
            new[] { false, true },
            accounts.OrderBy(account => account.Name)
                .Select(account => account.AllowNegativeBalance));
    }

    [Fact]
    public async Task Updating_account_policy_preserves_existing_negative_balance()
    {
        using var factory = new CustomWebApplicationFactory();
        var account = await SeedAccountAsync(
            factory,
            balance: -25m,
            allowNegativeBalance: true);
        using var client = CreateAuthenticatedClient(factory, account.UserId);

        var response = await client.PutAsJsonAsync(
            $"/api/Account/{account.Id}",
            new UpdateAccountDto
            {
                Name = account.Name,
                Type = account.Type,
                AllowNegativeBalance = false
            });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
        var updated = await dbContext.Accounts
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == account.Id);
        Assert.False(updated.AllowNegativeBalance);
        Assert.Equal(-25m, updated.CurrentBalance);
    }

    [Fact]
    public async Task Expense_rejects_insufficient_funds_without_changes_when_negative_balance_is_disabled()
    {
        using var factory = new CustomWebApplicationFactory();
        var account = await SeedAccountAsync(factory, balance: 10m);
        using var client = CreateAuthenticatedClient(factory, account.UserId);

        var response = await CreateTransactionAsync(
            client,
            account.Id,
            TransactionType.Expense,
            40m);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertAccountAndTransactionsAsync(factory, account.Id, 10m, expectedCount: 0);
    }

    [Fact]
    public async Task Expense_can_make_account_negative_when_enabled()
    {
        using var factory = new CustomWebApplicationFactory();
        var account = await SeedAccountAsync(
            factory,
            balance: 10m,
            allowNegativeBalance: true);
        using var client = CreateAuthenticatedClient(factory, account.UserId);

        var response = await CreateTransactionAsync(
            client,
            account.Id,
            TransactionType.Expense,
            40m);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await AssertAccountAndTransactionsAsync(factory, account.Id, -30m, expectedCount: 1);
    }

    [Fact]
    public async Task Income_creation_is_not_blocked_by_negative_balance_policy()
    {
        using var factory = new CustomWebApplicationFactory();
        var account = await SeedAccountAsync(factory, balance: -10m);
        using var client = CreateAuthenticatedClient(factory, account.UserId);

        var response = await CreateTransactionAsync(
            client,
            account.Id,
            TransactionType.Income,
            5m);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await AssertAccountAndTransactionsAsync(factory, account.Id, -5m, expectedCount: 1);
    }

    [Fact]
    public async Task Deleting_income_is_rejected_atomically_when_it_would_make_account_negative()
    {
        using var factory = new CustomWebApplicationFactory();
        var account = await SeedAccountAsync(factory, balance: 0m);
        using var client = CreateAuthenticatedClient(factory, account.UserId);
        var createResponse = await CreateTransactionAsync(
            client,
            account.Id,
            TransactionType.Income,
            50m);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        Guid transactionId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            var transaction = await dbContext.Transactions.SingleAsync();
            transactionId = transaction.Id;
            var persistedAccount = await dbContext.Accounts.SingleAsync(entity => entity.Id == account.Id);
            persistedAccount.CurrentBalance = 20m;
            await dbContext.SaveChangesAsync();
        }

        var deleteResponse = await client.DeleteAsync($"/api/Transaction/{transactionId}");

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
        await AssertAccountAndTransactionsAsync(factory, account.Id, 20m, expectedCount: 1);
    }

    [Fact]
    public async Task Deleting_income_can_make_account_negative_when_enabled()
    {
        using var factory = new CustomWebApplicationFactory();
        var account = await SeedAccountAsync(
            factory,
            balance: -10m,
            allowNegativeBalance: true);
        using var client = CreateAuthenticatedClient(factory, account.UserId);
        var createResponse = await CreateTransactionAsync(
            client,
            account.Id,
            TransactionType.Income,
            5m);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        Guid transactionId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            transactionId = await dbContext.Transactions
                .Select(transaction => transaction.Id)
                .SingleAsync();
        }

        var deleteResponse = await client.DeleteAsync($"/api/Transaction/{transactionId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        await AssertAccountAndTransactionsAsync(factory, account.Id, -10m, expectedCount: 0);
    }

    [Fact]
    public async Task Deleting_transfer_is_rejected_when_destination_cannot_refund_transfer()
    {
        using var factory = new CustomWebApplicationFactory();
        var source = await SeedAccountAsync(factory, balance: 100m);
        var destination = await AddAccountAsync(
            factory,
            source.UserId,
            balance: 0m,
            allowNegativeBalance: false);
        using var client = CreateAuthenticatedClient(factory, source.UserId);
        var createResponse = await client.PostAsJsonAsync(
            "/api/Transaction",
            new CreateTransactionDto
            {
                AccountId = source.Id,
                ToAccountId = destination.Id,
                Amount = 75m,
                Type = (int)TransactionType.Transfer,
                Description = "Delete policy",
                TransactionDate = new DateOnly(2026, 11, 4)
            });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        Guid transactionId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            transactionId = await dbContext.Transactions
                .Select(transaction => transaction.Id)
                .SingleAsync();
            var persistedDestination = await dbContext.Accounts
                .SingleAsync(entity => entity.Id == destination.Id);
            persistedDestination.CurrentBalance = 10m;
            await dbContext.SaveChangesAsync();
        }

        var deleteResponse = await client.DeleteAsync($"/api/Transaction/{transactionId}");

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
        Assert.Equal(25m, (await verifyDbContext.Accounts
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == source.Id)).CurrentBalance);
        Assert.Equal(10m, (await verifyDbContext.Accounts
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == destination.Id)).CurrentBalance);
        Assert.True(await verifyDbContext.Transactions
            .AsNoTracking()
            .AnyAsync(entity => entity.Id == transactionId));
    }

    [Fact]
    public async Task Deleting_transfer_can_make_destination_negative_when_enabled()
    {
        using var factory = new CustomWebApplicationFactory();
        var source = await SeedAccountAsync(factory, balance: 100m);
        var destination = await AddAccountAsync(
            factory,
            source.UserId,
            balance: -10m,
            allowNegativeBalance: true);
        using var client = CreateAuthenticatedClient(factory, source.UserId);
        var createResponse = await client.PostAsJsonAsync(
            "/api/Transaction",
            new CreateTransactionDto
            {
                AccountId = source.Id,
                ToAccountId = destination.Id,
                Amount = 75m,
                Type = (int)TransactionType.Transfer,
                Description = "Allowed transfer reversal",
                TransactionDate = new DateOnly(2026, 11, 4)
            });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        Guid transactionId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            transactionId = await dbContext.Transactions
                .Select(transaction => transaction.Id)
                .SingleAsync();
        }

        var deleteResponse = await client.DeleteAsync($"/api/Transaction/{transactionId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
        Assert.Equal(100m, (await verifyDbContext.Accounts
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == source.Id)).CurrentBalance);
        Assert.Equal(-10m, (await verifyDbContext.Accounts
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == destination.Id)).CurrentBalance);
        Assert.False(await verifyDbContext.Transactions
            .AsNoTracking()
            .AnyAsync(entity => entity.Id == transactionId));
    }

    private static async Task<Account> SeedAccountAsync(
        CustomWebApplicationFactory factory,
        decimal balance,
        bool allowNegativeBalance = false)
    {
        await factory.InitializeDatabaseAsync();
        var user = new User($"PolicyUser-{Guid.NewGuid():N}", "test-hash");
        var account = new Account(
            user.Id,
            "Policy account",
            AccountType.Cashflow,
            balance,
            allowNegativeBalance: allowNegativeBalance)
        {
            CurrentBalance = balance
        };

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
        dbContext.Users.Add(user);
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync();
        return account;
    }

    private static async Task<Account> AddAccountAsync(
        CustomWebApplicationFactory factory,
        Guid userId,
        decimal balance,
        bool allowNegativeBalance)
    {
        var account = new Account(
            userId,
            "Second account",
            AccountType.Cashflow,
            balance,
            allowNegativeBalance: allowNegativeBalance)
        {
            CurrentBalance = balance
        };
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync();
        return account;
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

    private static Task<HttpResponseMessage> CreateTransactionAsync(
        HttpClient client,
        Guid accountId,
        TransactionType type,
        decimal amount)
    {
        return client.PostAsJsonAsync(
            "/api/Transaction",
            new CreateTransactionDto
            {
                AccountId = accountId,
                Amount = amount,
                Type = (int)type,
                Description = "Account policy test",
                TransactionDate = new DateOnly(2026, 11, 4)
            });
    }

    private static async Task AssertAccountAndTransactionsAsync(
        CustomWebApplicationFactory factory,
        Guid accountId,
        decimal expectedBalance,
        int expectedCount)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
        var account = await dbContext.Accounts
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == accountId);
        Assert.Equal(expectedBalance, account.CurrentBalance);
        Assert.Equal(expectedCount, await dbContext.Transactions.CountAsync());
    }

    private static async Task<User> CreateUserAsync(CustomWebApplicationFactory factory)
    {
        var user = new User($"PolicyUser-{Guid.NewGuid():N}", "test-hash");
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return user;
    }
}
