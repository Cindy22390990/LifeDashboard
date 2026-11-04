using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Application.Services.Interfaces;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Application.Services;

public class GoalService : IGoalService
{
    private readonly IGoalRepository _goalRepository;

    public GoalService(IGoalRepository goalRepository)
    {
        _goalRepository = goalRepository;
    }

    public async Task<IReadOnlyList<GoalDto>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var goals = await _goalRepository.GetAllAsync(userId, cancellationToken);
        return goals.Select(ToDto).ToArray();
    }

    public async Task<GoalDto?> GetByIdAsync(
        int id,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var goal = await _goalRepository.GetByIdAsync(id, userId, cancellationToken);
        return goal is null ? null : ToDto(goal);
    }

    public async Task<GoalDto> CreateAsync(
        Guid userId,
        CreateGoalDto dto,
        CancellationToken cancellationToken)
    {
        var goal = new Goal(userId, dto.Name, dto.TargetAmount, dto.Tier, dto.GoalType, dto.Note);
        await _goalRepository.AddAsync(goal, cancellationToken);
        await _goalRepository.SaveChangesAsync(cancellationToken);
        return ToDto(goal);
    }

    public async Task UpdateAsync(
        int id,
        Guid userId,
        UpdateGoalDto dto,
        CancellationToken cancellationToken)
    {
        var goal = await GetOwnedGoalAsync(id, userId, cancellationToken);
        goal.UpdateDetails(dto.Name, dto.TargetAmount, dto.Tier, dto.GoalType, dto.Note);
        await _goalRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        int id,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var goal = await GetOwnedGoalAsync(id, userId, cancellationToken);
        if (goal.Status == GoalStatus.Completed)
        {
            throw new InvalidOperationException("已完成的目標不可直接刪除。");
        }

        _goalRepository.Remove(goal);
        await _goalRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<Goal> GetOwnedGoalAsync(
        int id,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _goalRepository.GetByIdAsync(id, userId, cancellationToken)
            ?? throw new KeyNotFoundException("找不到指定的目標。");
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
