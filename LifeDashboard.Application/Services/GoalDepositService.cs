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
    private readonly ITransferCore _transferCore;
    private readonly IUnitOfWork _unitOfWork;

    public GoalDepositService(
        IGoalRepository goalRepository,
        ITransferCore transferCore,
        IUnitOfWork unitOfWork)
    {
        _goalRepository = goalRepository;
        _transferCore = transferCore;
        _unitOfWork = unitOfWork;
    }

    public async Task<GoalDto> DepositAsync(
        int goalId,
        Guid userId,
        GoalDepositRequest request,
        CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
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

            var transfer = await _transferCore.CreateTransferAsync(
                userId,
                request.FromAccountId,
                request.ToAccountId,
                request.Amount,
                DateOnly.FromDateTime(DateTime.UtcNow),
                request.Note);

            transfer.Transaction.AssociateWithGoal(goal);
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
