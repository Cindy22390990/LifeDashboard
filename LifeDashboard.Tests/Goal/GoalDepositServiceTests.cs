using LifeDashboard.Application.Exceptions;
using LifeDashboard.Application.Interfaces;
using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Application.Services;
using LifeDashboard.Application.Services.Interfaces;
using LifeDashboard.Domain.Enums;
using Moq;
using GoalEntity = LifeDashboard.Domain.Entities.Goal;

namespace LifeDashboard.Tests.Goal;

public class GoalDepositServiceTests
{
    [Fact]
    public async Task DepositAsync_transfers_funds_and_completes_saving_goal_at_target()
    {
        var userId = Guid.NewGuid();
        var goal = CreateGoal(userId, GoalType.Saving, 100m);
        var (service, goalRepository, transactionService, unitOfWork) = CreateService(userId, goal);
        var request = CreateRequest(100m);

        var result = await service.DepositAsync(
            goal.Id,
            userId,
            request,
            CancellationToken.None);

        Assert.Equal(100m, result.CurrentAmount);
        Assert.Equal(GoalStatus.Completed, result.Status);
        Assert.NotNull(result.CompletedDate);
        goalRepository.Verify(
            repository => repository.GetByIdAsync(goal.Id, userId, CancellationToken.None),
            Times.Once);
        transactionService.Verify(
            transactionService => transactionService.CreateTransactionWithinExistingTransactionAsync(
                userId,
                It.Is<CreateTransactionDto>(dto =>
                    dto.AccountId == request.FromAccountId &&
                    dto.ToAccountId == request.ToAccountId &&
                    dto.Amount == request.Amount &&
                    dto.Type == (int)TransactionType.Transfer &&
                    dto.Description == request.Note),
                goal),
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
        var (service, _, _, _) = CreateService(userId, goal);

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
        var (service, _, transactionService, unitOfWork) = CreateService(
            userId,
            goal,
            new InsufficientFundsException("Insufficient funds."));

        await Assert.ThrowsAsync<InsufficientFundsException>(() =>
            service.DepositAsync(
                goal.Id,
                userId,
                CreateRequest(100m),
                CancellationToken.None));

        Assert.Equal(0m, goal.CurrentAmount);
        unitOfWork.Verify(unitOfWork => unitOfWork.BeginTransactionAsync(), Times.Once);
        unitOfWork.Verify(unitOfWork => unitOfWork.RollbackAsync(), Times.Once);
        unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
        unitOfWork.Verify(unitOfWork => unitOfWork.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task DepositAsync_rejects_completed_goal_without_starting_transaction()
    {
        var userId = Guid.NewGuid();
        var goal = CreateGoal(userId, GoalType.Saving, 100m);
        goal.ApplyDeposit(100m);
        var (service, _, transactionService, unitOfWork) = CreateService(userId, goal);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DepositAsync(
                goal.Id,
                userId,
                CreateRequest(10m),
                CancellationToken.None));

        transactionService.Verify(
            service => service.CreateTransactionWithinExistingTransactionAsync(
                It.IsAny<Guid>(),
                It.IsAny<CreateTransactionDto>(),
                It.IsAny<GoalEntity>()),
            Times.Never);
        unitOfWork.Verify(unitOfWork => unitOfWork.BeginTransactionAsync(), Times.Never);
        unitOfWork.Verify(unitOfWork => unitOfWork.RollbackAsync(), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DepositAsync_rejects_non_positive_amount_without_starting_transaction(
        decimal amount)
    {
        var userId = Guid.NewGuid();
        var goal = CreateGoal(userId, GoalType.Saving, 100m);
        var (service, _, transactionService, unitOfWork) = CreateService(userId, goal);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.DepositAsync(
            goal.Id,
            userId,
            CreateRequest(amount),
            CancellationToken.None));

        transactionService.Verify(
            service => service.CreateTransactionWithinExistingTransactionAsync(
                It.IsAny<Guid>(),
                It.IsAny<CreateTransactionDto>(),
                It.IsAny<GoalEntity>()),
            Times.Never);
        unitOfWork.Verify(unitOfWork => unitOfWork.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task DepositAsync_rejects_same_source_and_destination_without_starting_transaction()
    {
        var userId = Guid.NewGuid();
        var goal = CreateGoal(userId, GoalType.Saving, 100m);
        var (service, _, transactionService, unitOfWork) = CreateService(userId, goal);
        var accountId = Guid.NewGuid();
        var request = CreateRequest(10m) with
        {
            FromAccountId = accountId,
            ToAccountId = accountId
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.DepositAsync(
            goal.Id,
            userId,
            request,
            CancellationToken.None));

        transactionService.Verify(
            service => service.CreateTransactionWithinExistingTransactionAsync(
                It.IsAny<Guid>(),
                It.IsAny<CreateTransactionDto>(),
                It.IsAny<GoalEntity>()),
            Times.Never);
        unitOfWork.Verify(unitOfWork => unitOfWork.BeginTransactionAsync(), Times.Never);
    }

    private static (
        GoalDepositService Service,
        Mock<IGoalRepository> GoalRepository,
        Mock<ITransactionService> TransactionService,
        Mock<IUnitOfWork> UnitOfWork) CreateService(
        Guid userId,
        GoalEntity goal,
        Exception? transactionException = null)
    {
        var goalRepository = new Mock<IGoalRepository>();
        goalRepository
            .Setup(repository => repository.GetByIdAsync(
                goal.Id,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(goal);

        var transactionService = new Mock<ITransactionService>();
        var transactionSetup = transactionService
            .Setup(service => service.CreateTransactionWithinExistingTransactionAsync(
                userId,
                It.IsAny<CreateTransactionDto>(),
                goal));
        if (transactionException is null)
        {
            transactionSetup.ReturnsAsync(new TransactionDto());
        }
        else
        {
            transactionSetup.ThrowsAsync(transactionException);
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
            transactionService.Object,
            unitOfWork.Object);

        return (service, goalRepository, transactionService, unitOfWork);
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
