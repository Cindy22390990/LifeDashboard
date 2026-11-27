using LifeDashboard.Domain.Entities;

namespace LifeDashboard.Application.Repositories.Interfaces;

public interface IUserPreferenceRepository
{
    Task<UserPreference?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task SaveAsync(
        UserPreference preference,
        CancellationToken cancellationToken);
}
