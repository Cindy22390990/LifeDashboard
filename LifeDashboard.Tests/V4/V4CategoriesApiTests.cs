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

public sealed class V4CategoriesApiTests
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
    public async Task GetCategories_ShouldReturnReadDto_WhenAuthenticated()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        var user = new User("V4CategoriesRead", "test-hash");
        var category = new Category(
            user.Id,
            "Dining",
            "🍽",
            BucketType.Living);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            db.Users.Add(user);
            db.Categories.Add(category);
            await db.SaveChangesAsync();
        }

        using var client = CreateAuthenticatedClient(factory, user.Id);
        using var response = await client.GetAsync("/api/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content
            .ReadFromJsonAsync<CategoryReadDto[]>();

        Assert.NotNull(result);
        var item = Assert.Single(result);
        Assert.Equal(category.Id, item.Id);
        Assert.Equal("Dining", item.Name);
        Assert.Equal("🍽", item.Icon);
        Assert.Equal(BucketType.Living, item.BucketType);
        Assert.NotEqual(default, item.CreatedAt);
    }

    [Fact]
    public async Task GetCategories_ShouldReturnEmptyList_WhenUserHasNoCategories()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        var user = new User("V4CategoriesEmpty", "test-hash");

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        using var client = CreateAuthenticatedClient(factory, user.Id);
        using var response = await client.GetAsync("/api/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content
            .ReadFromJsonAsync<CategoryReadDto[]>();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCategories_ShouldNotLeakAnotherUsersCategories()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        var owner = new User("V4CategoriesOwner", "test-hash");
        var otherUser = new User("V4CategoriesOther", "test-hash");
        var ownerCategory = new Category(
            owner.Id,
            "Owner",
            "O",
            BucketType.Responsibility);
        var otherCategory = new Category(
            otherUser.Id,
            "Other",
            "X",
            BucketType.Entertainment);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
            db.Users.AddRange(owner, otherUser);
            db.Categories.AddRange(ownerCategory, otherCategory);
            await db.SaveChangesAsync();
        }

        using var client = CreateAuthenticatedClient(factory, owner.Id);
        using var response = await client.GetAsync("/api/categories");
        var result = await response.Content
            .ReadFromJsonAsync<CategoryReadDto[]>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        var item = Assert.Single(result);
        Assert.Equal(ownerCategory.Id, item.Id);
    }

    [Fact]
    public async Task GetCategories_ShouldReturnUnauthorized_WhenNoAuthentication()
    {
        using var factory = new CustomWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/categories");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
