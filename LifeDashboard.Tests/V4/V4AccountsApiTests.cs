using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LifeDashboard.Application.Features.V4.Dtos;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Domain.Enums;
using LifeDashboard.Infrastructure.Data;
using LifeDashboard.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace LifeDashboard.Tests.V4;

public sealed class V4AccountsApiTests
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
    public async Task GetAccounts_ShouldReturnReadModel_WhenAuthenticated()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        var user = new User("V4AccountsRead", "test-hash");
        var cashflow = new Account(
            user.Id,
            "Daily",
            AccountType.Cashflow,
            1_000m);
        var saving = new Account(
            user.Id,
            "Emergency",
            AccountType.Saving,
            5_000m);
        var investment = new Account(
            user.Id,
            "Brokerage",
            AccountType.Investment,
            12_000m);
        var deleted = new Account(
            user.Id,
            "Deleted",
            AccountType.Cashflow,
            999m);
        deleted.MarkAsDeleted();
        var system = new Account(
            user.Id,
            "System",
            AccountType.Cashflow,
            888m,
            isSystemAccount: true);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            db.Users.Add(user);
            db.Accounts.AddRange(cashflow, saving, investment, deleted, system);
            await db.SaveChangesAsync();
        }

        using var client = CreateAuthenticatedClient(factory, user.Id);
        using var response = await client.GetAsync("/api/accounts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AccountsReadDto>();

        Assert.NotNull(result);
        Assert.Equal(18_000m, result.TotalAssets);
        Assert.Equal(3, result.Items.Count);
        Assert.Contains(result.Items, account =>
            account.Id == investment.Id &&
            account.AccountType == "Invest");
        Assert.DoesNotContain(result.Items, account =>
            account.Id == deleted.Id ||
            account.Id == system.Id);
    }

    [Fact]
    public async Task GetAccounts_ShouldReturnEmptyReadModel_WhenNoEligibleAccounts()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        var user = new User("V4AccountsEmpty", "test-hash");
        var system = new Account(
            user.Id,
            "System",
            AccountType.Cashflow,
            100m,
            isSystemAccount: true);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            db.Users.Add(user);
            db.Accounts.Add(system);
            await db.SaveChangesAsync();
        }

        using var client = CreateAuthenticatedClient(factory, user.Id);
        using var response = await client.GetAsync("/api/accounts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AccountsReadDto>();

        Assert.NotNull(result);
        Assert.Equal(0m, result.TotalAssets);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetAccounts_ShouldReturnUnauthorized_WhenNoAuthentication()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/accounts");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAccounts_ShouldNotLeakAnotherUsersAccounts()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        var owner = new User("V4AccountsOwner", "test-hash");
        var otherUser = new User("V4AccountsOther", "test-hash");
        var ownerAccount = new Account(
            owner.Id,
            "Owner",
            AccountType.Cashflow,
            400m);
        var otherAccount = new Account(
            otherUser.Id,
            "Other",
            AccountType.Saving,
            900m);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            db.Users.AddRange(owner, otherUser);
            db.Accounts.AddRange(ownerAccount, otherAccount);
            await db.SaveChangesAsync();
        }

        using var client = CreateAuthenticatedClient(factory, owner.Id);
        using var response = await client.GetAsync("/api/accounts");
        var result = await response.Content.ReadFromJsonAsync<AccountsReadDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(400m, result.TotalAssets);
        Assert.Single(result.Items);
        Assert.Equal(ownerAccount.Id, result.Items[0].Id);
    }
}
