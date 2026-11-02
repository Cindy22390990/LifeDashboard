using LifeDashboard.Domain.Entities;

namespace LifeDashboard.Application.Repositories.Interfaces;

public interface ICategoryRepository
{
    Task<IEnumerable<Category>> GetAllAsync(Guid userId);
    Task<Category?> GetByIdAsync(Guid id, Guid userId);
    Task AddAsync(Category category);
    Task RemoveAsync(Category category);
    Task SaveChangesAsync();
}
