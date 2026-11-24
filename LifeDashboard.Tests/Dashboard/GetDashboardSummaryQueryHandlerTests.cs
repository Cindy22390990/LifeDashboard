using LifeDashboard.Application.Features.Dashboard.Interfaces;
using LifeDashboard.Application.Features.Dashboard.Models;
using LifeDashboard.Application.Features.Dashboard.Queries;
using LifeDashboard.Domain.Enums;
using Moq;

namespace LifeDashboard.Tests.Dashboard;

public sealed class GetDashboardSummaryQueryHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task HandleAsync_empty_projection_returns_empty_dto_state()
    {
        using var cancellation = new CancellationTokenSource();
        var reader = new Mock<IDashboardSummaryReader>(MockBehavior.Strict);
        reader
            .Setup(instance => instance.GetSummaryAsync(UserId, cancellation.Token))
            .ReturnsAsync(new DashboardSummaryProjection(
                0m,
                0m,
                new DateOnly(2026, 11, 23),
                Array.Empty<DashboardAccountProjection>(),
                Array.Empty<RecentTransactionProjection>()));

        var handler = new GetDashboardSummaryQueryHandler(reader.Object);

        var result = await handler.HandleAsync(
            new GetDashboardSummaryQuery(UserId),
            cancellation.Token);

        Assert.Equal(0m, result.TotalAssets);
        Assert.Equal(0m, result.TodayExpense);
        Assert.Equal(new DateOnly(2026, 11, 23), result.BusinessDate);
        Assert.Empty(result.Accounts);
        Assert.Empty(result.RecentTransactions);
        reader.Verify(
            instance => instance.GetSummaryAsync(UserId, cancellation.Token),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_maps_projection_totals_to_dto()
    {
        var accountId = Guid.NewGuid();
        var reader = new Mock<IDashboardSummaryReader>(MockBehavior.Strict);
        reader
            .Setup(instance => instance.GetSummaryAsync(
                UserId,
                CancellationToken.None))
            .ReturnsAsync(new DashboardSummaryProjection(
                1_250_000m,
                3_500m,
                new DateOnly(2026, 11, 23),
                new[]
                {
                    new DashboardAccountProjection(
                        accountId,
                        "Cash",
                        AccountType.Cashflow,
                        1_250_000m)
                },
                Array.Empty<RecentTransactionProjection>()));

        var handler = new GetDashboardSummaryQueryHandler(reader.Object);

        var result = await handler.HandleAsync(
            new GetDashboardSummaryQuery(UserId),
            CancellationToken.None);

        Assert.Equal(1_250_000m, result.TotalAssets);
        Assert.Equal(3_500m, result.TodayExpense);
        Assert.Equal(new DateOnly(2026, 11, 23), result.BusinessDate);
        var account = Assert.Single(result.Accounts);
        Assert.Equal(accountId, account.Id);
        Assert.Equal("Cash", account.Name);
        Assert.Equal(AccountType.Cashflow, account.Type);
        Assert.Equal("Cashflow", account.AccountType);
        Assert.Equal(1_250_000m, account.Balance);
        Assert.Empty(result.RecentTransactions);
    }

    [Fact]
    public async Task HandleAsync_maps_all_recent_transaction_projection_fields()
    {
        var transactionId = Guid.NewGuid();
        var transactionDate = new DateOnly(2026, 11, 23);
        var projection = new RecentTransactionProjection(
            transactionId,
            "Weekly groceries",
            "Food",
            "Cash",
            875m,
            TransactionType.Expense,
            transactionDate);
        var reader = new Mock<IDashboardSummaryReader>(MockBehavior.Strict);
        reader
            .Setup(instance => instance.GetSummaryAsync(
                UserId,
                CancellationToken.None))
            .ReturnsAsync(new DashboardSummaryProjection(
                20_000m,
                875m,
                new DateOnly(2026, 11, 23),
                Array.Empty<DashboardAccountProjection>(),
                new[] { projection }));

        var handler = new GetDashboardSummaryQueryHandler(reader.Object);

        var result = await handler.HandleAsync(
            new GetDashboardSummaryQuery(UserId),
            CancellationToken.None);

        var transaction = Assert.Single(result.RecentTransactions);
        Assert.Equal(transactionId, transaction.Id);
        Assert.Equal("Weekly groceries", transaction.Note);
        Assert.Equal("Food", transaction.CategoryName);
        Assert.Equal("Cash", transaction.AccountName);
        Assert.Equal(875m, transaction.Amount);
        Assert.Equal(TransactionType.Expense, transaction.Type);
        Assert.Equal("Expense", transaction.TransactionType);
        Assert.Equal(transactionDate, transaction.TransactionDate);
    }
}
