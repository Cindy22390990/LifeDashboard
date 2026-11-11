using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Models.Queries;
using LifeDashboard.Application.Models.ReadModels;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Domain.ValueObjects;

namespace LifeDashboard.Application.Services;

public sealed class BudgetRadarAssembler
{
    public BudgetRadarReadModel Assemble(
        DateOnly asOfDate,
        DateOnly periodStartDate,
        DateOnly periodEndDate,
        decimal periodIncome,
        BudgetExpenseSummary expenseSummary,
        GoalPurchaseAggregate? goalPurchaseAggregate,
        IEnumerable<FixedCommitment> activeCommitments)
    {
        var commitmentDetails = activeCommitments
            .Select(commitment => new FixedCommitmentDetailReadModel(
                commitment.Id,
                commitment.Name,
                commitment.CommitmentType,
                commitment.MonthlyAmount,
                commitment.StartMonth,
                commitment.EndMonth))
            .ToArray();

        var snapshot = BudgetSnapshot.Calculate(
            asOfDate,
            periodStartDate,
            periodEndDate,
            periodIncome,
            commitmentDetails.Sum(commitment => commitment.MonthlyAmount),
            expenseSummary.PeriodLivingExpense,
            expenseSummary.UncategorizedExpenseTotal,
            expenseSummary.TodayLivingExpense,
            goalPurchaseAggregate?.ExpenseTotal,
            goalPurchaseAggregate?.TransactionCount);

        return new BudgetRadarReadModel(
            snapshot,
            Array.AsReadOnly(commitmentDetails));
    }

    public RadarStateDto ToRadarStateDto(BudgetRadarReadModel readModel)
    {
        var snapshot = readModel.Snapshot;
        return new RadarStateDto
        {
            DailySafeLimit = snapshot.DailySafeLimit,
            TodaySpent = snapshot.TodayLivingExpense,
            TodayRemaining = snapshot.TodayRemaining,
            RemainingDays = snapshot.RemainingDays,
            TotalLivingBudget = snapshot.TotalLivingBudget,
            UncategorizedExpenseAmount = snapshot.UncategorizedExpenseTotal
        };
    }
}