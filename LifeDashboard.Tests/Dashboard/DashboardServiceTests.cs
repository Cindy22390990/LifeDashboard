using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Application.Services;
using LifeDashboard.Application.Services.Interfaces;
using LifeDashboard.Domain.Enums;
using Moq;

namespace LifeDashboard.Tests.Dashboard;

public class DashboardServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly CurrentDate = new(2026, 11, 10);

    [Fact]
    public async Task GetSummaryAsync_composes_results_in_order_without_mutating_sources()
    {
        var sequence = new MockSequence();
        var budgetService = new Mock<IBudgetRadarService>(MockBehavior.Strict);
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        var goalService = new Mock<IGoalService>(MockBehavior.Strict);
        var (reportRepository, transactionService) = CreateAdditionalServiceMocks();
        var budget = new RadarStateDto { DailySafeLimit = 125.50m };
        var account = new AccountDto
        {
            Id = Guid.NewGuid(),
            Name = "Checking",
            Type = AccountType.Cashflow,
            CurrentBalance = 500m
        };
        var goal = new GoalDto
        {
            Id = 42,
            Name = "Emergency fund",
            TargetAmount = 1_000m,
            CurrentAmount = 250m,
            GoalType = GoalType.Saving,
            Status = GoalStatus.Active
        };
        var accountSource = new List<AccountDto> { account };
        IReadOnlyList<GoalDto> goalSource = new List<GoalDto> { goal };
        using var cancellationSource = new CancellationTokenSource();

        budgetService.InSequence(sequence)
            .Setup(service => service.GetRadarStateAsync(UserId, CurrentDate))
            .ReturnsAsync(budget);
        accountService.InSequence(sequence)
            .Setup(service => service.GetAllAccountsAsync(UserId))
            .ReturnsAsync(accountSource);
        goalService.InSequence(sequence)
            .Setup(service => service.GetAllAsync(UserId, cancellationSource.Token))
            .ReturnsAsync(goalSource);

        var service = new DashboardService(
            budgetService.Object,
            accountService.Object,
            goalService.Object,
            reportRepository.Object,
            transactionService.Object);

        var result = await service.GetSummaryAsync(
            UserId,
            CurrentDate,
            cancellationSource.Token);

        Assert.Same(budget, result.Budget);
        Assert.Equal(accountSource, result.Accounts);
        Assert.Equal(goalSource, result.Goals);
        Assert.Same(account, result.Accounts[0]);
        Assert.Same(goal, result.Goals[0]);
        Assert.Single(accountSource);
        Assert.Single(goalSource);
        Assert.Equal(500m, accountSource[0].CurrentBalance);
        Assert.Equal(250m, goalSource[0].CurrentAmount);
    }

    [Fact]
    public async Task GetSummaryAsync_propagates_budget_failure_without_calling_later_services()
    {
        var expectedException = new InvalidOperationException("Budget query failed.");
        var budgetService = new Mock<IBudgetRadarService>(MockBehavior.Strict);
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        var goalService = new Mock<IGoalService>(MockBehavior.Strict);
        var (reportRepository, transactionService) = CreateAdditionalServiceMocks();
        budgetService
            .Setup(service => service.GetRadarStateAsync(UserId, CurrentDate))
            .ThrowsAsync(expectedException);

        var service = new DashboardService(
            budgetService.Object,
            accountService.Object,
            goalService.Object,
            reportRepository.Object,
            transactionService.Object);

        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetSummaryAsync(UserId, CurrentDate, CancellationToken.None));

        Assert.Same(expectedException, actualException);
        accountService.Verify(account => account.GetAllAccountsAsync(It.IsAny<Guid>()), Times.Never);
        goalService.Verify(
            goal => goal.GetAllAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetSummaryAsync_propagates_account_failure_without_calling_goal_service()
    {
        var expectedException = new InvalidOperationException("Account query failed.");
        var budgetService = new Mock<IBudgetRadarService>(MockBehavior.Strict);
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        var goalService = new Mock<IGoalService>(MockBehavior.Strict);
        var (reportRepository, transactionService) = CreateAdditionalServiceMocks();
        budgetService
            .Setup(service => service.GetRadarStateAsync(UserId, CurrentDate))
            .ReturnsAsync(new RadarStateDto());
        accountService
            .Setup(service => service.GetAllAccountsAsync(UserId))
            .ThrowsAsync(expectedException);

        var service = new DashboardService(
            budgetService.Object,
            accountService.Object,
            goalService.Object,
            reportRepository.Object,
            transactionService.Object);

        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetSummaryAsync(UserId, CurrentDate, CancellationToken.None));

        Assert.Same(expectedException, actualException);
        goalService.Verify(
            goal => goal.GetAllAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetSummaryAsync_propagates_goal_failure_without_returning_partial_summary()
    {
        var expectedException = new InvalidOperationException("Goal query failed.");
        var budgetService = new Mock<IBudgetRadarService>(MockBehavior.Strict);
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        var goalService = new Mock<IGoalService>(MockBehavior.Strict);
        var (reportRepository, transactionService) = CreateAdditionalServiceMocks();
        budgetService
            .Setup(service => service.GetRadarStateAsync(UserId, CurrentDate))
            .ReturnsAsync(new RadarStateDto { DailySafeLimit = 200m });
        accountService
            .Setup(service => service.GetAllAccountsAsync(UserId))
            .ReturnsAsync(new[]
            {
                new AccountDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Checking",
                    Type = AccountType.Cashflow,
                    CurrentBalance = 500m
                }
            });
        goalService
            .Setup(service => service.GetAllAsync(UserId, CancellationToken.None))
            .ThrowsAsync(expectedException);

        var service = new DashboardService(
            budgetService.Object,
            accountService.Object,
            goalService.Object,
            reportRepository.Object,
            transactionService.Object);

        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetSummaryAsync(UserId, CurrentDate, CancellationToken.None));

        Assert.Same(expectedException, actualException);
        budgetService.Verify(
            budget => budget.GetRadarStateAsync(UserId, CurrentDate),
            Times.Once);
        accountService.Verify(
            account => account.GetAllAccountsAsync(UserId),
            Times.Once);
        goalService.Verify(
            goal => goal.GetAllAsync(UserId, CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task GetSummaryAsync_aggregates_approved_dashboard_fields()
    {
        var budgetService = new Mock<IBudgetRadarService>(MockBehavior.Strict);
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        var goalService = new Mock<IGoalService>(MockBehavior.Strict);
        var (reportRepository, transactionService) = CreateAdditionalServiceMocks();
        var expectedTransactions = Enumerable.Range(0, 6)
            .Select(index => new TransactionDto
            {
                Id = Guid.NewGuid(),
                TransactionDate = CurrentDate.AddDays(-index),
                Description = $"Transaction {index}"
            })
            .ToArray();

        budgetService
            .Setup(service => service.GetRadarStateAsync(UserId, CurrentDate))
            .ReturnsAsync(new RadarStateDto());
        accountService
            .Setup(service => service.GetAllAccountsAsync(UserId))
            .ReturnsAsync(new[]
            {
                new AccountDto
                {
                    CurrentBalance = 1_000m,
                    IsSystemAccount = false
                },
                new AccountDto
                {
                    CurrentBalance = 9_000m,
                    IsSystemAccount = true
                }
            });
        goalService
            .Setup(service => service.GetAllAsync(UserId, CancellationToken.None))
            .ReturnsAsync(Array.Empty<GoalDto>());
        reportRepository
            .Setup(repository => repository.GetMonthlyTotalsAsync(
                UserId,
                new DateOnly(2026, 11, 1),
                new DateOnly(2026, 12, 1)))
            .ReturnsAsync((1_500m, 725m));
        transactionService
            .Setup(service => service.GetAllTransactionsAsync(UserId))
            .ReturnsAsync(expectedTransactions);

        var service = new DashboardService(
            budgetService.Object,
            accountService.Object,
            goalService.Object,
            reportRepository.Object,
            transactionService.Object);

        var result = await service.GetSummaryAsync(
            UserId,
            CurrentDate,
            CancellationToken.None);

        Assert.Equal(1_000m, result.TotalAssets);
        Assert.Equal(1_500m, result.MonthlyIncome);
        Assert.Equal(725m, result.MonthlyExpense);
        Assert.Equal(
            expectedTransactions.Take(5).Select(transaction => transaction.Id),
            result.RecentTransactions.Select(transaction => transaction.Id));
        reportRepository.Verify(repository => repository.GetMonthlyTotalsAsync(
                UserId,
                new DateOnly(2026, 11, 1),
                new DateOnly(2026, 12, 1)),
            Times.Once);
    }

    private static (
        Mock<IReportRepository> ReportRepository,
        Mock<ITransactionService> TransactionService) CreateAdditionalServiceMocks()
    {
        var reportRepository = new Mock<IReportRepository>(MockBehavior.Strict);
        reportRepository
            .Setup(repository => repository.GetMonthlyTotalsAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly?>()))
            .ReturnsAsync((0m, 0m));

        var transactionService = new Mock<ITransactionService>(MockBehavior.Strict);
        transactionService
            .Setup(service => service.GetAllTransactionsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(Array.Empty<TransactionDto>());

        return (reportRepository, transactionService);
    }
}