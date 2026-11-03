using LifeDashboard.Domain.Entities;
using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Application.Repositories.Interfaces
{
    public interface ITransactionRepository
    {
        Task<IEnumerable<Transaction>> GetAllAsync(
            Guid userId);
        Task<Transaction?> GetByIdAsync(
            Guid id,
            Guid userId);
        Task<decimal> GetTotalAmountAsync(
            Guid userId,
            DateOnly startDate,
            DateOnly endDate,
            TransactionType type,
            BucketType? bucketType = null);
        Task<decimal> GetUncategorizedExpenseTotalAsync(
            Guid userId,
            DateOnly startDate,
            DateOnly endDate);
        Task AddAsync(Transaction transaction);
        void Delete(Transaction transaction);
    }
}