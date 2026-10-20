using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LifeDashboard.Infrastructure.Repositories;

    // 帳戶資料存取實作
    // 實作 IAccountRepository，負責直接與 DbContext (SQL Server) 溝通
    public class AccountRepository : IAccountRepository
{
    private readonly LifeDashboardDbContext _context;

    public AccountRepository(LifeDashboardDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Account>> GetAllAsync()
    {
        return await _context.Accounts
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<Account?> GetByIdAsync(Guid id)
    {
        return await _context.Accounts.FindAsync(id);
    }

    public async Task AddAsync(Account account)
    {
        await _context.Accounts.AddAsync(account);
    }

    public async Task UpdateAsync(Account account)
    {
        _context.Accounts.Update(account);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == id);

        if (account == null)
        {
            throw new KeyNotFoundException("找不到指定帳戶。");
        }

        account.MarkAsDeleted();

        await _context.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}