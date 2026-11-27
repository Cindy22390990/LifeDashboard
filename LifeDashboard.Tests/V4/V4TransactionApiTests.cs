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

public sealed class V4TransactionApiTests
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
    public async Task QuickCreate_ShouldCreateTransaction_WhenValidRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        var user = new User("V4QuickValid", "test-hash");
        var account = new Account(
            user.Id,
            "Cashflow",
            AccountType.Cashflow,
            1_000m);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            db.Users.Add(user);
            db.Accounts.Add(account);
            await db.SaveChangesAsync();
        }

        using var client = CreateAuthenticatedClient(factory, user.Id);
        using var response = await client.PostAsJsonAsync(
            "/api/transactions/quick",
            new QuickCreateTransactionRequest(
                125m,
                "Lunch",
                (int)TransactionType.Expense));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<V4TransactionItemDto>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(account.Id, result.AccountId);
        Assert.Equal(125m, result.Amount);
    }

    [Fact]
    public async Task QuickCreate_ShouldResolveDefaultAccount_WhenAccountNotProvided()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        var user = new User("V4QuickAccount", "test-hash");
        var account = new Account(
            user.Id,
            "Default Cashflow",
            AccountType.Cashflow,
            2_000m);
        var preference = new UserPreference(
            user.Id,
            account.Id);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            db.Users.Add(user);
            db.Accounts.Add(account);
            db.UserPreferences.Add(preference);
            await db.SaveChangesAsync();
        }

        using var client = CreateAuthenticatedClient(factory, user.Id);
        using var response = await client.PostAsJsonAsync(
            "/api/transactions/quick",
            new QuickCreateTransactionRequest(
                80m,
                "Coffee",
                (int)TransactionType.Expense));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<V4TransactionItemDto>();
        Assert.NotNull(result);
        Assert.Equal(account.Id, result.AccountId);
    }

    [Fact]
    public async Task QuickCreate_ShouldResolveDefaultCategory_WhenCategoryNotProvided()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        var user = new User("V4QuickCategory", "test-hash");
        var account = new Account(
            user.Id,
            "Cashflow",
            AccountType.Cashflow,
            2_000m);
        var category = new Category(
            user.Id,
            "Dining",
            "🍽",
            BucketType.Living);
        var preference = new UserPreference(
            user.Id,
            account.Id,
            category.Id);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            db.Users.Add(user);
            db.Accounts.Add(account);
            db.Categories.Add(category);
            db.UserPreferences.Add(preference);
            await db.SaveChangesAsync();
        }

        using var client = CreateAuthenticatedClient(factory, user.Id);
        using var response = await client.PostAsJsonAsync(
            "/api/transactions/quick",
            new QuickCreateTransactionRequest(
                90m,
                "Dinner",
                (int)TransactionType.Expense));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<V4TransactionItemDto>();
        Assert.NotNull(result);
        Assert.Equal(category.Id, result.CategoryId);
    }

    [Fact]
    public async Task QuickCreate_ShouldReturnProblemDetails_WhenBalanceInsufficient()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        var user = new User("V4QuickInsufficient", "test-hash");
        var account = new Account(
            user.Id,
            "Low Balance",
            AccountType.Cashflow,
            50m);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            db.Users.Add(user);
            db.Accounts.Add(account);
            await db.SaveChangesAsync();
        }

        using var client = CreateAuthenticatedClient(factory, user.Id);
        using var response = await client.PostAsJsonAsync(
            "/api/transactions/quick",
            new QuickCreateTransactionRequest(
                100m,
                "Over balance",
                (int)TransactionType.Expense));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = await response.Content.ReadAsStreamAsync();
        using var document = await System.Text.Json.JsonDocument.ParseAsync(json);
        Assert.Equal(
            "https://api.lifedashboard/errors/insufficient-balance",
            document.RootElement.GetProperty("type").GetString());
        Assert.Equal(400, document.RootElement.GetProperty("status").GetInt32());
        Assert.True(document.RootElement.TryGetProperty("title", out _));
        Assert.True(document.RootElement.TryGetProperty("detail", out _));
    }

    [Fact]
    public async Task QuickCreate_ShouldReturn422ProblemDetails_WhenNoCashflowAccount()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        var user = new User("V4QuickNoCashflow", "test-hash");

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        using var client = CreateAuthenticatedClient(factory, user.Id);
        using var response = await client.PostAsJsonAsync(
            "/api/transactions/quick",
            new QuickCreateTransactionRequest(
                100m,
                "No account",
                (int)TransactionType.Expense));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        using var json = await response.Content.ReadAsStreamAsync();
        using var document = await System.Text.Json.JsonDocument.ParseAsync(json);
        Assert.Equal(
            "https://api.lifedashboard/errors/no-cashflow-account",
            document.RootElement.GetProperty("type").GetString());
        Assert.Equal(422, document.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task QuickCreate_ShouldRejectTransferTransaction()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        var user = new User("V4QuickTransfer", "test-hash");
        var account = new Account(
            user.Id,
            "Cashflow",
            AccountType.Cashflow,
            1_000m);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            db.Users.Add(user);
            db.Accounts.Add(account);
            await db.SaveChangesAsync();
        }

        using var client = CreateAuthenticatedClient(factory, user.Id);
        using var response = await client.PostAsJsonAsync(
            "/api/transactions/quick",
            new QuickCreateTransactionRequest(
                100m,
                "Transfer",
                (int)TransactionType.Transfer));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = await response.Content.ReadAsStreamAsync();
        using var document = await System.Text.Json.JsonDocument.ParseAsync(json);
        Assert.Equal(
            "https://api.lifedashboard/errors/validation-failed",
            document.RootElement.GetProperty("type").GetString());
    }

    [Fact]
    public async Task QuickCreate_ShouldReturnUnauthorized_WhenNoAuthentication()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/transactions/quick",
            new QuickCreateTransactionRequest(
                100m,
                "Unauthorized",
                (int)TransactionType.Expense));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_transactions_supports_filter_pagination_deleted_filter_and_totals()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        var user = new User("V4TransactionUser", "test-hash");
        var account = new Account(
            user.Id,
            "Cashflow",
            AccountType.Cashflow,
            10_000m);
        var category = new Category(
            user.Id,
            "Dining",
            "🍽",
            BucketType.Living);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var expense = new TransactionEntity(
            user.Id,
            account.Id,
            125m,
            TransactionType.Expense,
            "Lunch",
            today);
        expense.CategoryId = category.Id;
        var income = new TransactionEntity(
            user.Id,
            account.Id,
            300m,
            TransactionType.Income,
            "Salary",
            today);
        var deleted = new TransactionEntity(
            user.Id,
            account.Id,
            999m,
            TransactionType.Expense,
            "Deleted",
            today);
        deleted.MarkAsDeleted("test");

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            db.Users.Add(user);
            db.Accounts.Add(account);
            db.Categories.Add(category);
            db.Transactions.AddRange(expense, income, deleted);
            await db.SaveChangesAsync();
        }

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                CustomWebApplicationFactory.CreateJwt(user.Id));

        using var response = await client.GetAsync(
            $"/api/transactions?year={today.Year}&month={today.Month}&page=1&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(responseBody);
        Assert.False(document.RootElement.TryGetProperty("totalCount", out _));
        Assert.False(document.RootElement.TryGetProperty("page", out _));
        Assert.False(document.RootElement.TryGetProperty("pageSize", out _));
        var result = JsonSerializer.Deserialize<TransactionListResponseDto>(
            responseBody,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(result);
        Assert.Equal(300m, result.TotalIncome);
        Assert.Equal(125m, result.TotalExpense);
        Assert.Single(result.Items);

        using var secondPageResponse = await client.GetAsync(
            $"/api/transactions?year={today.Year}&month={today.Month}&page=2&pageSize=1");
        Assert.Equal(HttpStatusCode.OK, secondPageResponse.StatusCode);
        var secondPage = await secondPageResponse.Content
            .ReadFromJsonAsync<TransactionListResponseDto>();
        Assert.NotNull(secondPage);
        Assert.Single(secondPage.Items);
        Assert.NotEqual(result.Items[0].Id, secondPage.Items[0].Id);

        using var thirdPageResponse = await client.GetAsync(
            $"/api/transactions?year={today.Year}&month={today.Month}&page=3&pageSize=1");
        Assert.Equal(HttpStatusCode.OK, thirdPageResponse.StatusCode);
        var thirdPage = await thirdPageResponse.Content
            .ReadFromJsonAsync<TransactionListResponseDto>();
        Assert.NotNull(thirdPage);
        Assert.Empty(thirdPage.Items);
    }

    [Fact]
    public async Task Get_transactions_applies_all_requested_filters_and_sorting()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        var user = new User("V4TransactionFilters", "test-hash");
        var account = new Account(
            user.Id,
            "Cashflow",
            AccountType.Cashflow,
            10_000m);
        var otherAccount = new Account(
            user.Id,
            "Other Cashflow",
            AccountType.Cashflow,
            10_000m);
        var category = new Category(
            user.Id,
            "Dining",
            "🍽",
            BucketType.Living);
        var otherCategory = new Category(
            user.Id,
            "Transport",
            "🚗",
            BucketType.Living);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);
        var higherExpense = new TransactionEntity(
            user.Id,
            account.Id,
            300m,
            TransactionType.Expense,
            "Lunch with team",
            yesterday);
        higherExpense.CategoryId = category.Id;
        var lowerExpense = new TransactionEntity(
            user.Id,
            account.Id,
            100m,
            TransactionType.Expense,
            "Lunch",
            today);
        lowerExpense.CategoryId = category.Id;
        var income = new TransactionEntity(
            user.Id,
            account.Id,
            500m,
            TransactionType.Income,
            "Lunch reimbursement",
            today);
        income.CategoryId = category.Id;
        var otherAccountExpense = new TransactionEntity(
            user.Id,
            otherAccount.Id,
            200m,
            TransactionType.Expense,
            "Lunch",
            today);
        otherAccountExpense.CategoryId = category.Id;
        var otherCategoryExpense = new TransactionEntity(
            user.Id,
            account.Id,
            250m,
            TransactionType.Expense,
            "Lunch",
            today);
        otherCategoryExpense.CategoryId = otherCategory.Id;

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            db.Users.Add(user);
            db.Accounts.AddRange(account, otherAccount);
            db.Categories.AddRange(category, otherCategory);
            db.Transactions.AddRange(
                higherExpense,
                lowerExpense,
                income,
                otherAccountExpense,
                otherCategoryExpense);
            await db.SaveChangesAsync();
        }

        using var client = CreateAuthenticatedClient(factory, user.Id);
        var query =
            $"/api/transactions?page=1&pageSize=10&keyword=Lunch" +
            $"&accountId={account.Id}&categoryId={category.Id}" +
            $"&transactionType=Expense&startDate={yesterday:yyyy-MM-dd}" +
            $"&endDate={today:yyyy-MM-dd}&sortBy=amount&sortDirection=asc";

        using var response = await client.GetAsync(query);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content
            .ReadFromJsonAsync<TransactionListResponseDto>();

        Assert.NotNull(result);
        Assert.Equal(400m, result.TotalExpense);
        Assert.Equal(0m, result.TotalIncome);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(100m, result.Items[0].Amount);
        Assert.Equal(300m, result.Items[1].Amount);
        Assert.All(
            result.Items,
            item =>
            {
                Assert.Equal(account.Id, item.AccountId);
                Assert.Equal(category.Id, item.CategoryId);
            });
    }

    [Theory]
    [InlineData("transactionType", "Unknown")]
    [InlineData("transactionType", "3")]
    [InlineData("sortBy", "createdAt")]
    public async Task Get_transactions_returns_problem_details_for_unsupported_query_values(
        string parameter,
        string value)
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        using var client = CreateAuthenticatedClient(factory, Guid.NewGuid());

        using var response = await client.GetAsync(
            $"/api/transactions?{parameter}={Uri.EscapeDataString(value)}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }
}
