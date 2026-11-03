using LifeDashboard.Application.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace LifeDashboard.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly LifeDashboardDbContext _context;
    private IDbContextTransaction? _currentTransaction;

    public UnitOfWork(LifeDashboardDbContext context)
    {
        _context = context;
    }

    public async Task BeginTransactionAsync()
    {
        if (_currentTransaction != null)
        {
            throw new InvalidOperationException("資料庫交易已啟動。");
        }

        _currentTransaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        var transaction = _currentTransaction
            ?? throw new InvalidOperationException("目前沒有可提交的資料庫交易。");

        await transaction.CommitAsync();
        _currentTransaction = null;
        await transaction.DisposeAsync();
    }

    public async Task RollbackAsync()
    {
        var transaction = _currentTransaction;
        if (transaction == null)
        {
            return;
        }

        try
        {
            await transaction.RollbackAsync();
        }
        finally
        {
            _currentTransaction = null;
            await transaction.DisposeAsync();
        }
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}
