using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Domain.Entities;
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

        public async Task<Transaction> AddAsync(Transaction transaction)
        {
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<Transaction> AddTransferAsync(Transaction transaction)
        {
            await using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();
                return transaction;
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateAsync(Transaction transaction)
        {
            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Transaction transaction)
        {
            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteTransferAsync(Transaction transaction)
        {
            await using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Transactions.Remove(transaction);
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }
    }
}