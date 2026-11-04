using LifeDashboard.Application.Exceptions;
using LifeDashboard.Application.Interfaces;
using LifeDashboard.Application.Models;
using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Application.Services;
using LifeDashboard.Application.Services.Interfaces;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Domain.Enums;
using Moq;
using GoalEntity = LifeDashboard.Domain.Entities.Goal;
using TransactionEntity = LifeDashboard.Domain.Entities.Transaction;

namespace LifeDashboard.Tests.Goal;

public class GoalDepositServiceTests
{
    [Fact]
    public async Task DepositAsync_transfers_funds_and_completes_saving_goal_at_target()
    {
        var userId = Guid.NewGuid();
        var goal = CreateGoal(userId, GoalType.Saving, 100m);
        var transfer = CreateTransfer(userId, 100m);
        var (service, goalRepository, transferCore, unitOfWork) = CreateService(
            userId,
            goal,
            transfer);

        var result = await service.DepositAsync(
            goal.Id,
            userId,
            CreateRequest(100m),
            CancellationToken.None);

        Assert.Equal(100m, result.CurrentAmount);
        Assert.Equal(GoalStatus.Completed, result.Status);
        Assert.NotNull(result.CompletedDate);
        Assert.Equal(goal.Id, transfer.Transaction.GoalId);
        Assert.Same(goal, transfer.Transaction.Goal);
        goalRepository.Verify(
            repository => repository.GetByIdAsync(goal.Id, userId, CancellationToken.None),
            Times.Once);
        transferCore.Verify(
            core => core.CreateTransferAsync(
                userId,
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                100m,
                It.IsAny<DateOnly>(),
                "Goal deposit"),
            Times.Once);
        unitOfWork.Verify(unitOfWork => unitOfWork.BeginTransactionAsync(), Times.Once);
        unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Once);
        unitOfWork.Verify(unitOfWork => unitOfWork.CommitAsync(), Times.Once);
        unitOfWork.Verify(unitOfWork => unitOfWork.RollbackAsync(), Times.Never);
    }

    [Fact]
    public async Task DepositAsync_does_not_complete_purchase_goal_when_target_is_reached()
    {
        var userId = Guid.NewGuid();
        var goal = CreateGoal(userId, GoalType.Purchase, 100m);
        var transfer = CreateTransfer(userId, 100m);
        var (service, _, _, _) = CreateService(userId, goal, transfer);

        var result = await service.DepositAsync(
            goal.Id,
            userId,
            CreateRequest(100m),
            CancellationToken.None);

        Assert.Equal(100m, result.CurrentAmount);
        Assert.Equal(GoalStatus.Active, result.Status);
        Assert.Null(result.CompletedDate);
    }

    [Fact]
    public async Task DepositAsync_rolls_back_when_transfer_fails()
    {
        var userId = Guid.NewGuid();
        var goal = CreateGoal(userId, GoalType.Saving, 100m);
        var (service, _, transferCore, unitOfWork) = CreateService(
            userId,
            goal,
            transfer: null);
        transferCore
            .Setup(core => core.CreateTransferAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<decimal>(),
                It.IsAny<DateOnly>(),
                It.IsAny<string>()))
            .ThrowsAsync(new InsufficientFundsException("Insufficient funds."));

        await Assert.ThrowsAsync<InsufficientFundsException>(() =>
            service.DepositAsync(
                goal.Id,
                userId,
                CreateRequest(100m),
                CancellationToken.None));

        Assert.Equal(0m, goal.CurrentAmount);
        unitOfWork.Verify(unitOfWork => unitOfWork.RollbackAsync(), Times.Once);
        unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
        unitOfWork.Verify(unitOfWork => unitOfWork.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task DepositAsync_rejects_completed_goal_and_rolls_back()
    {
        var userId = Guid.NewGuid();
        var goal = CreateGoal(userId, GoalType.Saving, 100m);
        goal.ApplyDeposit(100m);
        var (service, _, transferCore, unitOfWork) = CreateService(
            userId,
            goal,
            transfer: null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DepositAsync(
                goal.Id,
                userId,
                CreateRequest(10m),
                CancellationToken.None));

        transferCore.Verify(
            core => core.CreateTransferAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<decimal>(),
                It.IsAny<DateOnly>(),
                It.IsAny<string>()),
            Times.Never);
        unitOfWork.Verify(unitOfWork => unitOfWork.RollbackAsync(), Times.Once);
    }

    private static (
        GoalDepositService Service,
        Mock<IGoalRepository> GoalRepository,
        Mock<ITransferCore> TransferCore,
        Mock<IUnitOfWork> UnitOfWork) CreateService(
        Guid userId,
        GoalEntity goal,
        TransferCoreResult? transfer)
    {
        var goalRepository = new Mock<IGoalRepository>();
        goalRepository
            .Setup(repository => repository.GetByIdAsync(
                goal.Id,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(goal);

        var transferCore = new Mock<ITransferCore>();
        if (transfer is not null)
        {
            transferCore
                .Setup(core => core.CreateTransferAsync(
                    userId,
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<decimal>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<string>()))
                .ReturnsAsync(transfer);
        }

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(work => work.BeginTransactionAsync())
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(work => work.SaveChangesAsync())
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(work => work.CommitAsync())
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(work => work.RollbackAsync())
            .Returns(Task.CompletedTask);

        var service = new GoalDepositService(
            goalRepository.Object,
            transferCore.Object,
            unitOfWork.Object);

        return (service, goalRepository, transferCore, unitOfWork);
    }

    private static GoalEntity CreateGoal(
        Guid userId,
        GoalType goalType,
        decimal targetAmount)
    {
        return new GoalEntity(
            userId,
            "Test goal",
            targetAmount,
            GoalTier.Tier1_Safety,
            goalType,
            string.Empty);
    }

    private static TransferCoreResult CreateTransfer(Guid userId, decimal amount)
    {
        var sourceAccount = new Account(
            userId,
            "Source",
            AccountType.Cashflow,
            200m);
        var destinationAccount = new Account(
            userId,
            "Goal",
            AccountType.Cashflow,
            0m);
        var transaction = new TransactionEntity(
            userId,
            sourceAccount.Id,
            amount,
            TransactionType.Transfer,
            "Goal deposit",
            new DateOnly(2026, 11, 3),
            destinationAccount.Id);

        return new TransferCoreResult(transaction, sourceAccount, destinationAccount);
    }

    private static GoalDepositRequest CreateRequest(decimal amount)
    {
        return new GoalDepositRequest
        {
            Amount = amount,
            FromAccountId = Guid.NewGuid(),
            ToAccountId = Guid.NewGuid(),
            Note = "Goal deposit"
        };
    }
}
