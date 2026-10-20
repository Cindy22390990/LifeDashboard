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

        public async Task<IEnumerable<Transaction>> GetAllAsync()
        {
            return await _context.Transactions
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.CreatedAtUtc)
                .ToListAsync();
        }

        public async Task<Transaction?> GetByIdAsync(Guid id)
        {
            return await _context.Transactions.FindAsync(id);
        }

        public async Task<Transaction> AddAsync(Transaction transaction)
        {
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            return transaction;
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
    }
}