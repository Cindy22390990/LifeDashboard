using LifeDashboard.Domain.Entities;

namespace LifeDashboard.Application.Repositories.Interfaces;

public interface IGoalRepository
{
    Task<IReadOnlyList<Goal>> GetAllAsync(Guid userId, CancellationToken cancellationToken);
    Task<Goal?> GetByIdAsync(int id, Guid userId, CancellationToken cancellationToken);
    Task AddAsync(Goal goal, CancellationToken cancellationToken);
    void Remove(Goal goal);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
