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

        public Task<Guid?> GetRecentCategoryIdAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            return _context.Transactions
                .AsNoTracking()
                .Where(transaction =>
                    transaction.UserId == userId &&
                    transaction.Type == TransactionType.Expense &&
                    transaction.CategoryId.HasValue)
                .OrderByDescending(transaction => transaction.TransactionDate)
                .ThenByDescending(transaction => transaction.CreatedAtUtc)
                .Select(transaction => transaction.CategoryId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Guid?> GetMostUsedCategoryIdAsync(
            Guid userId,
            DateOnly startDate,
            CancellationToken cancellationToken)
        {
            var result = await _context.Transactions
                .AsNoTracking()
                .Where(transaction =>
                    transaction.UserId == userId &&
                    transaction.CategoryId.HasValue &&
                    transaction.TransactionDate >= startDate)
                .GroupBy(transaction => transaction.CategoryId!.Value)
                .Select(group => new
                {
                    CategoryId = group.Key,
                    Count = group.Count()
                })
                .OrderByDescending(item => item.Count)
                .ThenByDescending(item => item.CategoryId)
                .FirstOrDefaultAsync(cancellationToken);

            return result?.CategoryId;
        }

        public async Task<Guid?> GetMostUsedExpenseAccountIdAsync(
            Guid userId,
            DateOnly startDate,
            CancellationToken cancellationToken)
        {
            var result = await _context.Transactions
                .AsNoTracking()
                .Where(transaction =>
                    transaction.UserId == userId &&
                    transaction.Type == TransactionType.Expense &&
                    transaction.Account != null &&
                    transaction.Account.UserId == userId &&
                    !transaction.Account.IsDeleted &&
                    !transaction.Account.IsSystemAccount &&
                    transaction.Account.Type == AccountType.Cashflow &&
                    transaction.TransactionDate >= startDate)
                .GroupBy(transaction => new
                {
                    transaction.AccountId,
                    transaction.Account.CreatedAtUtc
                })
                .Select(group => new
                {
                    group.Key.AccountId,
                    group.Key.CreatedAtUtc,
                    Count = group.Count()
                })
                .OrderByDescending(item => item.Count)
                .ThenBy(item => item.CreatedAtUtc)
                .ThenBy(item => item.AccountId)
                .FirstOrDefaultAsync(cancellationToken);

            return result?.AccountId;
        }

        public async Task AddAsync(Transaction transaction)
        {
            await _context.Transactions.AddAsync(transaction);
        }

        public void Delete(Transaction transaction)
        {
            _context.Transactions.Update(transaction);
        }
    }
}