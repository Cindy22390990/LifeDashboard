using FluentAssertions;
using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Models.Queries;
using LifeDashboard.Domain.Enums;
using LifeDashboard.Tests.Helpers;
using Moq;

namespace LifeDashboard.Tests.BudgetRadar;

public class BudgetRadarServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task PaydayEve_UsesPreviousPayrollPeriodAndStartMonth()
    {
        var context = TestDataFactory.CreateBudgetRadarService(payday: 5);

        await context.Service.GetRadarStateAsync(UserId, new DateOnly(2026, 11, 4));

        context.TransactionRepository.Verify(repository => repository.GetTotalAmountAsync(
                UserId,
                new DateOnly(2026, 10, 5),
                new DateOnly(2026, 11, 4),
                TransactionType.Income,
                null),
            Times.Once);
        context.BudgetRadarQueryRepository.Verify(repository => repository.GetExpenseSummaryAsync(
                UserId,
                new DateOnly(2026, 10, 5),
                new DateOnly(2026, 11, 4),
                new DateOnly(2026, 11, 4)),
            Times.Once);
        context.BudgetRadarQueryRepository.Verify(repository => repository.GetGoalPurchaseAggregateAsync(
                UserId,
                new DateOnly(2026, 10, 5),
                new DateOnly(2026, 11, 4)),
            Times.Once);
        context.FixedCommitmentRepository.Verify(repository =>
                repository.GetActiveForPayrollPeriodAsync(
                    UserId,
                    new DateOnly(2026, 10, 1)),
            Times.Once);
    }

    [Fact]
    public async Task Payday_StartsNewPayrollPeriod()
    {
        var context = TestDataFactory.CreateBudgetRadarService(payday: 5);

        await context.Service.GetRadarStateAsync(UserId, new DateOnly(2026, 11, 5));

        context.TransactionRepository.Verify(repository => repository.GetTotalAmountAsync(
                UserId,
                new DateOnly(2026, 11, 5),
                new DateOnly(2026, 12, 4),
                TransactionType.Income,
                null),
            Times.Once);
            context.BudgetRadarQueryRepository.Verify(repository => repository.GetExpenseSummaryAsync(
                UserId,
                new DateOnly(2026, 11, 5),
                new DateOnly(2026, 12, 4),
                new DateOnly(2026, 11, 5)),
                Times.Once);
        context.FixedCommitmentRepository.Verify(repository =>
                repository.GetActiveForPayrollPeriodAsync(
                    UserId,
                    new DateOnly(2026, 11, 1)),
            Times.Once);
    }

    [Fact]
    public async Task DayAfterPayday_UsesCurrentPayrollPeriod()
    {
        var context = TestDataFactory.CreateBudgetRadarService(payday: 5);

        await context.Service.GetRadarStateAsync(UserId, new DateOnly(2026, 11, 6));

        context.TransactionRepository.Verify(repository => repository.GetTotalAmountAsync(
                UserId,
                new DateOnly(2026, 11, 5),
                new DateOnly(2026, 12, 4),
                TransactionType.Income,
                null),
            Times.Once);
            context.BudgetRadarQueryRepository.Verify(repository => repository.GetExpenseSummaryAsync(
                UserId,
                new DateOnly(2026, 11, 5),
                new DateOnly(2026, 12, 4),
                new DateOnly(2026, 11, 6)),
                Times.Once);
    }

    [Fact]
    public async Task FixedCommitments_ReturnedForPayrollMonthAreDeducted()
    {
        var context = TestDataFactory.CreateBudgetRadarService(payday: 5);
        var commitments = new[]
        {
            TestDataFactory.CreateFixedCommitment(
                1_000m,
                new DateOnly(2026, 9, 1)),
            TestDataFactory.CreateFixedCommitment(
                500m,
                new DateOnly(2026, 10, 1)),
            TestDataFactory.CreateFixedCommitment(
                300m,
                new DateOnly(2026, 8, 1),
                new DateOnly(2026, 10, 1)),
            TestDataFactory.CreateFixedCommitment(
                700m,
                new DateOnly(2026, 11, 1)),
            TestDataFactory.CreateFixedCommitment(
                200m,
                new DateOnly(2026, 8, 1),
                new DateOnly(2026, 9, 1))
        };
        context.TransactionRepository
            .Setup(repository => repository.GetTotalAmountAsync(
                UserId,
                new DateOnly(2026, 10, 5),
                new DateOnly(2026, 11, 4),
                TransactionType.Income,
                null))
            .ReturnsAsync(10_000m);
        context.FixedCommitmentRepository
            .Setup(repository => repository.GetActiveForPayrollPeriodAsync(
                UserId,
                It.IsAny<DateOnly>()))
            .Returns((Guid _, DateOnly payrollPeriodMonth) =>
                Task.FromResult<IEnumerable<LifeDashboard.Domain.Entities.FixedCommitment>>(
                    commitments.Where(commitment =>
                        commitment.StartMonth <= payrollPeriodMonth &&
                        (!commitment.EndMonth.HasValue ||
                         commitment.EndMonth.Value >= payrollPeriodMonth))
                    .ToArray()));

        var result = await context.Service.GetRadarStateAsync(
            UserId,
            new DateOnly(2026, 11, 2));

        result.TotalLivingBudget.Should().Be(8_200m);
        context.FixedCommitmentRepository.Verify(repository =>
                repository.GetActiveForPayrollPeriodAsync(
                    UserId,
                    new DateOnly(2026, 10, 1)),
            Times.Once);
    }

    [Fact]
    public async Task Income_IsQueriedOnlyForCurrentPayrollPeriodAndIncomeType()
    {
        var context = TestDataFactory.CreateBudgetRadarService(payday: 5);
        context.TransactionRepository
            .Setup(repository => repository.GetTotalAmountAsync(
                UserId,
                new DateOnly(2026, 10, 5),
                new DateOnly(2026, 11, 4),
                TransactionType.Income,
                null))
            .ReturnsAsync(2_000m);

        var result = await context.Service.GetRadarStateAsync(
            UserId,
            new DateOnly(2026, 11, 2));

        result.TotalLivingBudget.Should().Be(2_000m);
        context.TransactionRepository.Verify(repository => repository.GetTotalAmountAsync(
                UserId,
                new DateOnly(2026, 10, 5),
                new DateOnly(2026, 11, 4),
                TransactionType.Income,
                null),
            Times.Once);
        context.TransactionRepository.Verify(repository => repository.GetTotalAmountAsync(
                UserId,
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                TransactionType.Income,
                null),
            Times.Once);
    }

    [Fact]
    public async Task LivingExpenses_OnlyQueryExpenseWithLivingBucket()
    {
        var context = TestDataFactory.CreateBudgetRadarService(payday: 5);
        context.TransactionRepository
            .Setup(repository => repository.GetTotalAmountAsync(
                UserId,
                new DateOnly(2026, 10, 5),
                new DateOnly(2026, 11, 4),
                TransactionType.Income,
                null))
            .ReturnsAsync(1_000m);
        context.BudgetRadarQueryRepository
            .Setup(repository => repository.GetExpenseSummaryAsync(
                UserId,
                new DateOnly(2026, 10, 5),
                new DateOnly(2026, 11, 4),
                new DateOnly(2026, 11, 2)))
            .ReturnsAsync(new BudgetExpenseSummary(300m, 0m, 0m));

        var result = await context.Service.GetRadarStateAsync(
            UserId,
            new DateOnly(2026, 11, 2));

        result.DailySafeLimit.Should().Be(233.33m);
        context.BudgetRadarQueryRepository.Verify(repository => repository.GetExpenseSummaryAsync(
                UserId,
                new DateOnly(2026, 10, 5),
                new DateOnly(2026, 11, 4),
                new DateOnly(2026, 11, 2)),
            Times.Once);
    }

    [Fact]
    public async Task UncategorizedExpenses_AreReportedButDoNotReduceLivingBudget()
    {
        var context = TestDataFactory.CreateBudgetRadarService(payday: 5);
        context.TransactionRepository
            .Setup(repository => repository.GetTotalAmountAsync(
                UserId,
                new DateOnly(2026, 10, 5),
                new DateOnly(2026, 11, 4),
                TransactionType.Income,
                null))
            .ReturnsAsync(300m);
        context.BudgetRadarQueryRepository
            .Setup(repository => repository.GetExpenseSummaryAsync(
                UserId,
                new DateOnly(2026, 10, 5),
                new DateOnly(2026, 11, 4),
                new DateOnly(2026, 11, 2)))
            .ReturnsAsync(new BudgetExpenseSummary(0m, 0m, 125m));

        var result = await context.Service.GetRadarStateAsync(
            UserId,
            new DateOnly(2026, 11, 2));

        result.TotalLivingBudget.Should().Be(300m);
        result.DailySafeLimit.Should().Be(100m);
        result.UncategorizedExpenseAmount.Should().Be(125m);
        context.BudgetRadarQueryRepository.Verify(repository =>
            repository.GetExpenseSummaryAsync(
                    UserId,
                    new DateOnly(2026, 10, 5),
                new DateOnly(2026, 11, 4),
                new DateOnly(2026, 11, 2)),
            Times.Once);
    }

    [Fact]
    public async Task NegativeRemainingLivingBudget_IsNotClampedToZero()
    {
        var context = TestDataFactory.CreateBudgetRadarService(payday: 5);
        context.TransactionRepository
            .Setup(repository => repository.GetTotalAmountAsync(
                UserId,
                new DateOnly(2026, 10, 5),
                new DateOnly(2026, 11, 4),
                TransactionType.Income,
                null))
            .ReturnsAsync(100m);
        context.BudgetRadarQueryRepository
            .Setup(repository => repository.GetExpenseSummaryAsync(
                UserId,
                new DateOnly(2026, 10, 5),
                new DateOnly(2026, 11, 4),
                new DateOnly(2026, 11, 2)))
            .ReturnsAsync(new BudgetExpenseSummary(200m, 0m, 0m));

        var result = await context.Service.GetRadarStateAsync(
            UserId,
            new DateOnly(2026, 11, 2));

        result.DailySafeLimit.Should().BeLessThan(0m);
    }

    [Fact]
    public async Task DailySafeLimit_RoundsToTwoDecimalPlaces()
    {
        var context = TestDataFactory.CreateBudgetRadarService(payday: 30);
        context.TransactionRepository
            .Setup(repository => repository.GetTotalAmountAsync(
                UserId,
                new DateOnly(2026, 10, 30),
                new DateOnly(2026, 11, 29),
                TransactionType.Income,
                null))
            .ReturnsAsync(10_000m);

        var result = await context.Service.GetRadarStateAsync(
            UserId,
            new DateOnly(2026, 10, 30));

        result.RemainingDays.Should().Be(31);
        result.DailySafeLimit.Should().Be(322.58m);
    }

    [Fact]
    public async Task TodayRemaining_CanBeNegative()
    {
        var context = TestDataFactory.CreateBudgetRadarService(payday: 5);
        context.TransactionRepository
            .Setup(repository => repository.GetTotalAmountAsync(
                UserId,
                new DateOnly(2026, 11, 5),
                new DateOnly(2026, 12, 4),
                TransactionType.Income,
                null))
            .ReturnsAsync(1_575m);
        context.BudgetRadarQueryRepository
            .Setup(repository => repository.GetExpenseSummaryAsync(
                UserId,
                new DateOnly(2026, 11, 5),
                new DateOnly(2026, 12, 4),
                new DateOnly(2026, 11, 5)))
            .ReturnsAsync(new BudgetExpenseSummary(75m, 75m, 0m));

        var result = await context.Service.GetRadarStateAsync(
            UserId,
            new DateOnly(2026, 11, 5));

        result.DailySafeLimit.Should().Be(50m);
        result.TodaySpent.Should().Be(75m);
        result.TodayRemaining.Should().Be(-25m);
    }

    [Fact]
    public async Task GoalPurchaseUnavailable_RemainsNullAndDoesNotChangeExistingRadarResponse()
    {
        var context = TestDataFactory.CreateBudgetRadarService(payday: 5);
        context.TransactionRepository
            .Setup(repository => repository.GetTotalAmountAsync(
                UserId,
                new DateOnly(2026, 10, 5),
                new DateOnly(2026, 11, 4),
                TransactionType.Income,
                null))
            .ReturnsAsync(900m);
        context.BudgetRadarQueryRepository
            .Setup(repository => repository.GetGoalPurchaseAggregateAsync(
                UserId,
                new DateOnly(2026, 10, 5),
                new DateOnly(2026, 11, 4)))
            .ReturnsAsync((GoalPurchaseAggregate?)null);

        var result = await context.Service.GetRadarStateAsync(
            UserId,
            new DateOnly(2026, 11, 2));

        result.TotalLivingBudget.Should().Be(900m);
        result.DailySafeLimit.Should().Be(300m);
        context.BudgetRadarQueryRepository.Verify(repository => repository.GetGoalPurchaseAggregateAsync(
                UserId,
                new DateOnly(2026, 10, 5),
                new DateOnly(2026, 11, 4)),
            Times.Once);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(75, 1)]
    public async Task GoalPurchaseAggregate_WhenAvailableDoesNotChangeExistingRadarResponse(
        decimal expenseTotal,
        int transactionCount)
    {
        var context = TestDataFactory.CreateBudgetRadarService(payday: 5);
        context.TransactionRepository
            .Setup(repository => repository.GetTotalAmountAsync(
                UserId,
                new DateOnly(2026, 10, 5),
                new DateOnly(2026, 11, 4),
                TransactionType.Income,
                null))
            .ReturnsAsync(900m);
        context.BudgetRadarQueryRepository
            .Setup(repository => repository.GetGoalPurchaseAggregateAsync(
                UserId,
                new DateOnly(2026, 10, 5),
                new DateOnly(2026, 11, 4)))
            .ReturnsAsync(new GoalPurchaseAggregate(expenseTotal, transactionCount));

        var result = await context.Service.GetRadarStateAsync(
            UserId,
            new DateOnly(2026, 11, 2));

        result.TotalLivingBudget.Should().Be(900m);
        result.DailySafeLimit.Should().Be(300m);
        result.GetType().GetProperties().Select(property => property.Name)
            .Should().Equal(
                "DailySafeLimit",
                "TodaySpent",
                "TodayRemaining",
                "RemainingDays",
                "TotalLivingBudget",
                "UncategorizedExpenseAmount");
    }
}
