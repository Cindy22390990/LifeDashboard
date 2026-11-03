using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Domain.Enums;
using LifeDashboard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LifeDashboard.Infrastructure.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly LifeDashboardDbContext _context;

        public TransactionRepository(LifeDashboardDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Transaction>> GetAllAsync(
            Guid userId)
        {
            return await _context.Transactions
                .Include(t => t.Account)
                .Include(t => t.ToAccount)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        public async Task<Transaction?> GetByIdAsync(
            Guid id,
            Guid userId)
        {
            return await _context.Transactions
                .FirstOrDefaultAsync(
                    t =>
                    t.Id == id &&
                    t.UserId == userId);
        }

        public async Task<decimal> GetTotalAmountAsync(
            Guid userId,
            DateOnly startDate,
            DateOnly endDate,
            TransactionType type,
            BucketType? bucketType = null)
        {
            var query = _context.Transactions
                .Where(transaction =>
                    transaction.UserId == userId &&
                    transaction.TransactionDate >= startDate &&
                    transaction.TransactionDate <= endDate &&
                    transaction.Type == type);

            if (bucketType.HasValue)
            {
                query = query.Where(transaction =>
                    transaction.CategoryEntity != null &&
                    transaction.CategoryEntity.BucketType == bucketType.Value);
            }

            return await query.SumAsync(transaction => (decimal?)transaction.Amount) ?? 0m;
        }

        public async Task<decimal> GetUncategorizedExpenseTotalAsync(
            Guid userId,
            DateOnly startDate,
            DateOnly endDate)
        {
            return await _context.Transactions
                .Where(transaction =>
                    transaction.UserId == userId &&
                    transaction.TransactionDate >= startDate &&
                    transaction.TransactionDate <= endDate &&
                    transaction.Type == TransactionType.Expense &&
                    transaction.CategoryId == null)
                .SumAsync(transaction => (decimal?)transaction.Amount) ?? 0m;
        }

        public async Task AddAsync(Transaction transaction)
        {
            await _context.Transactions.AddAsync(transaction);
        }

        public void Delete(Transaction transaction)
        {
            _context.Transactions.Remove(transaction);
        }
    }
}