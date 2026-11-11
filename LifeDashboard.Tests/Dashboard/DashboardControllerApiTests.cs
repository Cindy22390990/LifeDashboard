using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using LifeDashboard.Api;
using LifeDashboard.Api.Controllers;
using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Models.ReadModels;
using LifeDashboard.Application.Services.Interfaces;
using LifeDashboard.Domain.Enums;
using LifeDashboard.Tests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace LifeDashboard.Tests.Dashboard;

public class DashboardControllerApiTests
{
    [Fact]
    public async Task GetSummary_returns_composed_dashboard_response()
    {
        var userId = Guid.NewGuid();
        var budget = new RadarStateDto
        {
            DailySafeLimit = 200m,
            TodaySpent = 50m,
            TodayRemaining = 150m,
            RemainingDays = 5,
            TotalLivingBudget = 1_000m,
            UncategorizedExpenseAmount = 10m
        };
        var account = new AccountDto
        {
            Id = Guid.NewGuid(),
            Name = "Checking",
            Type = AccountType.Cashflow,
            CurrentBalance = 500m,
            IsSystemAccount = false,
            AllowNegativeBalance = false
        };
        var goal = new GoalDto
        {
            Id = 17,
            Name = "Emergency fund",
            TargetAmount = 2_000m,
            CurrentAmount = 800m,
            Tier = GoalTier.Tier1_Safety,
            GoalType = GoalType.Saving,
            Status = GoalStatus.Active,
            Note = "Reserve",
            CompletedDate = null
        };
        var dashboardService = new Mock<IDashboardService>();
        dashboardService
            .Setup(service => service.GetSummaryAsync(
                userId,
                new DateOnly(2026, 11, 10),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardSummaryReadModel(
                budget,
                new[] { account },
                new[] { goal }));
        using var factory = CreateFactory(dashboardService.Object);
        using var client = CreateAuthenticatedClient(factory, userId);

        var response = await client.GetAsync(
            "/api/dashboard/summary?currentDate=2026-11-10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal(200m, root.GetProperty("budget").GetProperty("dailySafeLimit").GetDecimal());
        Assert.Equal(50m, root.GetProperty("budget").GetProperty("todaySpent").GetDecimal());
        Assert.Equal("Checking", root.GetProperty("accounts")[0].GetProperty("name").GetString());
        Assert.Equal(500m, root.GetProperty("accounts")[0].GetProperty("currentBalance").GetDecimal());
        Assert.Equal("Emergency fund", root.GetProperty("goals")[0].GetProperty("name").GetString());
        Assert.Equal(800m, root.GetProperty("goals")[0].GetProperty("currentAmount").GetDecimal());

        dashboardService.Verify(service => service.GetSummaryAsync(
                userId,
                new DateOnly(2026, 11, 10),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetSummary_service_failure_is_handled_by_pipeline_without_partial_response()
    {
        var userId = Guid.NewGuid();
        var dashboardService = new Mock<IDashboardService>();
        dashboardService
            .Setup(service => service.GetSummaryAsync(
                userId,
                new DateOnly(2026, 11, 10),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Dashboard service failure marker."));
        using var factory = CreateFactory(dashboardService.Object);
        using var client = CreateAuthenticatedClient(factory, userId);

        var response = await client.GetAsync(
            "/api/dashboard/summary?currentDate=2026-11-10");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("\"budget\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"accounts\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"goals\"", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DashboardSummaryDto_has_only_composed_component_properties()
    {
        var properties = typeof(DashboardSummaryDto)
            .GetProperties()
            .Select(property => property.Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.Equal(new[] { "Accounts", "Budget", "Goals" }, properties);
        Assert.Equal(typeof(RadarStateDto), typeof(DashboardSummaryDto).GetProperty("Budget")!.PropertyType);
        Assert.Equal(
            typeof(IReadOnlyList<AccountDto>),
            typeof(DashboardSummaryDto).GetProperty("Accounts")!.PropertyType);
        Assert.Equal(
            typeof(IReadOnlyList<GoalDto>),
            typeof(DashboardSummaryDto).GetProperty("Goals")!.PropertyType);
    }

    private static HttpClient CreateAuthenticatedClient(
        WebApplicationFactory<Program> factory,
        Guid userId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CustomWebApplicationFactory.CreateJwt(userId));
        return client;
    }

    private static WebApplicationFactory<Program> CreateFactory(
        IDashboardService dashboardService)
    {
        return new CustomWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IDashboardService>();
                services.AddSingleton(dashboardService);
            });
        });
    }
}