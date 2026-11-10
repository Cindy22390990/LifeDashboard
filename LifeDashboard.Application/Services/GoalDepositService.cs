using LifeDashboard.Application.Interfaces;
using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Application.Services.Interfaces;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Application.Services;

public sealed class GoalDepositService : IGoalDepositService
{
    private readonly IGoalRepository _goalRepository;
    private readonly ITransactionService _transactionService;
    private readonly IUnitOfWork _unitOfWork;

    public GoalDepositService(
        IGoalRepository goalRepository,
        ITransactionService transactionService,
        IUnitOfWork unitOfWork)
    {
        _goalRepository = goalRepository;
        _transactionService = transactionService;
        _unitOfWork = unitOfWork;
    }

    public async Task<GoalDto> DepositAsync(
        int goalId,
        Guid userId,
        GoalDepositRequest request,
        CancellationToken cancellationToken)
    {
        var goal = await _goalRepository.GetByIdAsync(
            goalId,
            userId,
            cancellationToken)
            ?? throw new KeyNotFoundException("找不到指定的目標。");

        if (goal.Status == GoalStatus.Completed)
        {
            throw new InvalidOperationException("已完成的目標不可再存入資金。");
        }

        if (request.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Amount), "存入金額必須大於 0。");
        }

        if (request.FromAccountId == request.ToAccountId)
        {
            throw new ArgumentException("來源帳戶與轉入帳戶不可相同。", nameof(request.ToAccountId));
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _transactionService.CreateTransactionWithinExistingTransactionAsync(
                userId,
                new CreateTransactionDto
                {
                    AccountId = request.FromAccountId,
                    ToAccountId = request.ToAccountId,
                    Amount = request.Amount,
                    Type = (int)TransactionType.Transfer,
                    Description = request.Note,
                    TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow)
                },
                goal);

            goal.ApplyDeposit(request.Amount);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            return ToDto(goal);
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    private static GoalDto ToDto(Goal goal)
    {
        return new GoalDto
        {
            Id = goal.Id,
            Name = goal.Name,
            TargetAmount = goal.TargetAmount,
            CurrentAmount = goal.CurrentAmount,
            Tier = goal.Tier,
            GoalType = goal.GoalType,
            Status = goal.Status,
            Note = goal.Note,
            CompletedDate = goal.CompletedDate
        };
    }
}
