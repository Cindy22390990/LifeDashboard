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
        Task AddAsync(Transaction transaction);
        void Delete(Transaction transaction);
    }
}