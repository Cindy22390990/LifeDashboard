using LifeDashboard.Application.Models.DTOs;

namespace LifeDashboard.Application.Services.Interfaces;

public interface IGoalDepositService
{
    Task<GoalDto> DepositAsync(
        int goalId,
        Guid userId,
        GoalDepositRequest request,
        CancellationToken cancellationToken);
}
