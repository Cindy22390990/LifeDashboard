using LifeDashboard.Domain.Entities;

namespace LifeDashboard.Application.Repositories.Interfaces;

public interface IAccountRepository
{
    Task<IEnumerable<Account>> GetAllAsync();

    Task<Account?> GetByIdAsync(Guid id);

    Task AddAsync(Account account);

    Task UpdateAsync(Account account);

    Task DeleteAsync(Guid id);
    
    Task SaveChangesAsync();
}