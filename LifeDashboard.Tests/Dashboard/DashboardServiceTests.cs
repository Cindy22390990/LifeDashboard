using LifeDashboard.Application.Models.DTOs;
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
            goalService.Object);

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
        budgetService
            .Setup(service => service.GetRadarStateAsync(UserId, CurrentDate))
            .ThrowsAsync(expectedException);

        var service = new DashboardService(
            budgetService.Object,
            accountService.Object,
            goalService.Object);

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
        budgetService
            .Setup(service => service.GetRadarStateAsync(UserId, CurrentDate))
            .ReturnsAsync(new RadarStateDto());
        accountService
            .Setup(service => service.GetAllAccountsAsync(UserId))
            .ThrowsAsync(expectedException);

        var service = new DashboardService(
            budgetService.Object,
            accountService.Object,
            goalService.Object);

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
            goalService.Object);

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
}