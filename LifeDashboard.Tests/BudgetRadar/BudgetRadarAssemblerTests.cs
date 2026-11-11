using FluentAssertions;
using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Models.Queries;
using LifeDashboard.Application.Services;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Tests.BudgetRadar;

public class BudgetRadarAssemblerTests
{
    [Fact]
    public void Assemble_projects_commitments_builds_snapshot_and_maps_existing_dto()
    {
        var userId = Guid.NewGuid();
        var commitment = new FixedCommitment(
            userId,
            "Rent",
            1_000m,
            CommitmentType.Rent,
            new DateOnly(2026, 9, 1),
            null);
        var assembler = new BudgetRadarAssembler();

        var readModel = assembler.Assemble(
            new DateOnly(2026, 11, 2),
            new DateOnly(2026, 10, 5),
            new DateOnly(2026, 11, 4),
            periodIncome: 10_000m,
            new BudgetExpenseSummary(3_000m, 600m, 250m),
            new GoalPurchaseAggregate(75m, 1),
            new[] { commitment });
        var dto = assembler.ToRadarStateDto(readModel);

        Assert.Equal(9_000m, readModel.Snapshot.TotalLivingBudget);
        Assert.Equal(6_000m, readModel.Snapshot.RemainingLivingBudget);
        Assert.Equal(2_000m, readModel.Snapshot.DailySafeLimit);
        Assert.Equal(75m, readModel.Snapshot.GoalPurchaseExpenseTotal);
        Assert.Equal(1, readModel.Snapshot.GoalPurchaseCount);
        Assert.Single(readModel.ActiveCommitments);
        Assert.Equal(commitment.Id, readModel.ActiveCommitments[0].Id);
        Assert.Equal("Rent", readModel.ActiveCommitments[0].Name);
        Assert.Equal(1_000m, readModel.ActiveCommitments[0].MonthlyAmount);

        Assert.Equal(2_000m, dto.DailySafeLimit);
        Assert.Equal(600m, dto.TodaySpent);
        Assert.Equal(1_400m, dto.TodayRemaining);
        Assert.Equal(3, dto.RemainingDays);
        Assert.Equal(9_000m, dto.TotalLivingBudget);
        Assert.Equal(250m, dto.UncategorizedExpenseAmount);
        Assert.Equal(
            new[]
            {
                nameof(RadarStateDto.DailySafeLimit),
                nameof(RadarStateDto.TodaySpent),
                nameof(RadarStateDto.TodayRemaining),
                nameof(RadarStateDto.RemainingDays),
                nameof(RadarStateDto.TotalLivingBudget),
                nameof(RadarStateDto.UncategorizedExpenseAmount)
            },
            typeof(RadarStateDto).GetProperties().Select(property => property.Name));
    }

    [Fact]
    public void Assemble_preserves_unavailable_and_successful_empty_goal_purchase_states()
    {
        var assembler = new BudgetRadarAssembler();
        var asOfDate = new DateOnly(2026, 11, 2);
        var periodStartDate = new DateOnly(2026, 10, 5);
        var periodEndDate = new DateOnly(2026, 11, 4);
        var expenseSummary = new BudgetExpenseSummary(0m, 0m, 0m);
        var noCommitments = Array.Empty<FixedCommitment>();

        var unavailable = assembler.Assemble(
            asOfDate,
            periodStartDate,
            periodEndDate,
            0m,
            expenseSummary,
            goalPurchaseAggregate: null,
            noCommitments);
        var successfullyQueriedEmpty = assembler.Assemble(
            asOfDate,
            periodStartDate,
            periodEndDate,
            0m,
            expenseSummary,
            new GoalPurchaseAggregate(0m, 0),
            noCommitments);

        Assert.Null(unavailable.Snapshot.GoalPurchaseExpenseTotal);
        Assert.Null(unavailable.Snapshot.GoalPurchaseCount);
        Assert.Equal(0m, successfullyQueriedEmpty.Snapshot.GoalPurchaseExpenseTotal);
        Assert.Equal(0, successfullyQueriedEmpty.Snapshot.GoalPurchaseCount);
    }
}