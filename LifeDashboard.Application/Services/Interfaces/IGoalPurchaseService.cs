using LifeDashboard.Application.Models.DTOs;

namespace LifeDashboard.Application.Services.Interfaces;

public interface IGoalPurchaseService
{
    Task<GoalDto> PurchaseAsync(
        int goalId,
        Guid userId,
        GoalPurchaseRequest request,
        CancellationToken cancellationToken);
}
