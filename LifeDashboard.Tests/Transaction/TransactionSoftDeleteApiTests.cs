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

namespace LifeDashboard.Tests.Transaction;

public class TransactionSoftDeleteApiTests
{
    [Fact]
    public async Task Deleting_transaction_with_missing_owned_account_preserves_account_and_transaction()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        var transactionOwner = new User($"SoftDeleteOwner-{Guid.NewGuid():N}", "test-hash");
        var accountOwner = new User($"SoftDeleteAccountOwner-{Guid.NewGuid():N}", "test-hash");
        var account = new Account(
            accountOwner.Id,
            "Other user's account",
            AccountType.Cashflow,
            250m);
        var transaction = new TransactionEntity(
            transactionOwner.Id,
            account.Id,
            25m,
            TransactionType.Expense,
            "Missing owned account test",
            new DateOnly(2026, 11, 16));

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            dbContext.Users.AddRange(transactionOwner, accountOwner);
            dbContext.Accounts.Add(account);
            dbContext.Transactions.Add(transaction);
            await dbContext.SaveChangesAsync();
        }

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CustomWebApplicationFactory.CreateJwt(transactionOwner.Id));

        var deleteResponse = await client.DeleteAsync($"/api/Transaction/{transaction.Id}");

        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);

        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDbContext = verifyScope.ServiceProvider
            .GetRequiredService<LifeDashboardDbContext>();
        var persistedAccount = await verifyDbContext.Accounts
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == account.Id);
        var persistedTransaction = await verifyDbContext.Transactions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == transaction.Id);

        Assert.Equal(250m, persistedAccount.CurrentBalance);
        Assert.False(persistedTransaction.IsDeleted);
        Assert.Null(persistedTransaction.DeletedAt);
        Assert.Null(persistedTransaction.DeletedReason);
    }

    [Fact]
    public async Task Deleting_expense_restores_balance_and_preserves_hidden_audit_record()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        var user = new User($"SoftDeleteUser-{Guid.NewGuid():N}", "test-hash");
        var account = new Account(
            user.Id,
            "Expense account",
            AccountType.Cashflow,
            10_000m);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            dbContext.Users.Add(user);
            dbContext.Accounts.Add(account);
            await dbContext.SaveChangesAsync();
        }

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CustomWebApplicationFactory.CreateJwt(user.Id));

        var createResponse = await client.PostAsJsonAsync(
            "/api/Transaction",
            new CreateTransactionDto
            {
                AccountId = account.Id,
                Amount = 1_000m,
                Type = (int)TransactionType.Expense,
                Description = "Soft delete audit test",
                TransactionDate = new DateOnly(2026, 11, 16)
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createdTransaction = await createResponse.Content
            .ReadFromJsonAsync<TransactionDto>();
        Assert.NotNull(createdTransaction);

        var deleteResponse = await client.DeleteAsync(
            $"/api/Transaction/{createdTransaction.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var listResponse = await client.GetAsync("/api/Transaction");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var visibleTransactions = await listResponse.Content
            .ReadFromJsonAsync<List<TransactionDto>>();
        Assert.NotNull(visibleTransactions);
        Assert.Empty(visibleTransactions);

        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDbContext = verifyScope.ServiceProvider
            .GetRequiredService<LifeDashboardDbContext>();
        var persistedAccount = await verifyDbContext.Accounts
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == account.Id);
        var persistedTransaction = await verifyDbContext.Transactions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == createdTransaction.Id);

        Assert.Equal(10_000m, persistedAccount.CurrentBalance);
        Assert.True(persistedTransaction.IsDeleted);
        Assert.NotNull(persistedTransaction.DeletedAt);
        Assert.Equal("User deleted transaction", persistedTransaction.DeletedReason);

        using var repeatedDeleteResponse = await client.DeleteAsync(
            $"/api/Transaction/{createdTransaction.Id}");
        Assert.Equal(HttpStatusCode.NotFound, repeatedDeleteResponse.StatusCode);
    }
}
