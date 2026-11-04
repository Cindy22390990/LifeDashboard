using LifeDashboard.Application.Models.DTOs;

namespace LifeDashboard.Application.Services.Interfaces;

public interface IGoalService
{
    Task<IReadOnlyList<GoalDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken);
    Task<GoalDto?> GetByIdAsync(int id, Guid userId, CancellationToken cancellationToken);
    Task<GoalDto> CreateAsync(
        Guid userId,
        CreateGoalDto dto,
        CancellationToken cancellationToken);
    Task UpdateAsync(
        int id,
        Guid userId,
        UpdateGoalDto dto,
        CancellationToken cancellationToken);
    Task DeleteAsync(int id, Guid userId, CancellationToken cancellationToken);
}
