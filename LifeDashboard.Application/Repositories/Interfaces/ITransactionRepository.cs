using LifeDashboard.Domain.Entities;

namespace LifeDashboard.Application.Repositories.Interfaces
{
    public interface ITransactionRepository
    {
        Task<IEnumerable<Transaction>> GetAllAsync(
            Guid userId);
        Task<Transaction?> GetByIdAsync(
            Guid id,
            Guid userId);
        Task<Transaction> AddAsync(Transaction transaction);
        Task UpdateAsync(Transaction transaction);
        Task DeleteAsync(Transaction transaction);
    }
}