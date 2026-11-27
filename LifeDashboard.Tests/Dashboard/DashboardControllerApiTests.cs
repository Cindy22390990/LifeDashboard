using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using LifeDashboard.Api;
using LifeDashboard.Api.Controllers;
using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Models.ReadModels;
using LifeDashboard.Application.Services.Interfaces;
using LifeDashboard.Application.Features.V4.Dtos;
using LifeDashboard.Application.Features.V4.Services;
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
    public async Task GetSummary_returns_frozen_v4_summary_response()
    {
        var userId = Guid.NewGuid();
        var summaryService = new Mock<IV4DashboardSummaryService>();
        summaryService
            .Setup(service => service.GetSummaryAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardSummaryReadDto(
                500m,
                2_500m,
                750m));
        using var factory = CreateFactory(summaryService.Object);
        using var client = CreateAuthenticatedClient(factory, userId);

        var response = await client.GetAsync(
            "/api/dashboard/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal(3, root.EnumerateObject().Count());
        Assert.Equal(500m, root.GetProperty("totalAssets").GetDecimal());
        Assert.Equal(2_500m, root.GetProperty("monthlyIncome").GetDecimal());
        Assert.Equal(750m, root.GetProperty("monthlyExpense").GetDecimal());
        Assert.False(root.TryGetProperty("balance", out _));
        Assert.False(root.TryGetProperty("netWorth", out _));
        Assert.False(root.TryGetProperty("recentActivity", out _));

        summaryService.Verify(service => service.GetSummaryAsync(
                userId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetSummary_service_failure_is_handled_by_pipeline_without_partial_response()
    {
        var userId = Guid.NewGuid();
        var summaryService = new Mock<IV4DashboardSummaryService>();
        summaryService
            .Setup(service => service.GetSummaryAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Dashboard service failure marker."));
        using var factory = CreateFactory(summaryService.Object);
        using var client = CreateAuthenticatedClient(factory, userId);

        var response = await client.GetAsync(
            "/api/dashboard/summary");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("\"totalAssets\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"monthlyIncome\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"monthlyExpense\"", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DashboardSummaryDto_has_only_composed_component_properties()
    {
        var properties = typeof(DashboardSummaryDto)
            .GetProperties()
            .Select(property => property.Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "Accounts",
                "Budget",
                "Goals",
                "MonthlyExpense",
                "MonthlyIncome",
                "RecentTransactions",
                "TotalAssets"
            },
            properties);
        Assert.Equal(typeof(RadarStateDto), typeof(DashboardSummaryDto).GetProperty("Budget")!.PropertyType);
        Assert.Equal(
            typeof(IReadOnlyList<AccountDto>),
            typeof(DashboardSummaryDto).GetProperty("Accounts")!.PropertyType);
        Assert.Equal(
            typeof(IReadOnlyList<GoalDto>),
            typeof(DashboardSummaryDto).GetProperty("Goals")!.PropertyType);
        Assert.Equal(
            typeof(decimal),
            typeof(DashboardSummaryDto).GetProperty("TotalAssets")!.PropertyType);
        Assert.Equal(
            typeof(decimal),
            typeof(DashboardSummaryDto).GetProperty("MonthlyIncome")!.PropertyType);
        Assert.Equal(
            typeof(decimal),
            typeof(DashboardSummaryDto).GetProperty("MonthlyExpense")!.PropertyType);
        Assert.Equal(
            typeof(IReadOnlyList<TransactionDto>),
            typeof(DashboardSummaryDto).GetProperty("RecentTransactions")!.PropertyType);
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
        IV4DashboardSummaryService summaryService)
    {
        return new CustomWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IV4DashboardSummaryService>();
                services.AddSingleton(summaryService);
            });
        });
    }
}