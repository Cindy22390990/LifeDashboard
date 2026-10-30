using LifeDashboard.Domain.Entities;

namespace LifeDashboard.Application.Repositories.Interfaces;

public interface IAccountRepository
{

    Task<IEnumerable<Account>> GetAllAsync(
        Guid userId);


    Task<Account?> GetByIdAsync(
        Guid id,
        Guid userId);


    Task AddAsync(
        Account account);


    Task UpdateAsync(
        Account account);


    Task DeleteAsync(
        Guid id,
        Guid userId);


    Task SaveChangesAsync();

}