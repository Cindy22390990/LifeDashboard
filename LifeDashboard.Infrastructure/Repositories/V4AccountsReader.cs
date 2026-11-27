using LifeDashboard.Application.Features.V4.Interfaces;
using LifeDashboard.Application.Features.V4.Models;
using LifeDashboard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LifeDashboard.Infrastructure.Repositories;

public sealed class V4AccountsReader : IV4AccountsReader
{
    private readonly LifeDashboardDbContext _context;

    public V4AccountsReader(LifeDashboardDbContext context)
    {
        _context = context;
    }

    public async Task<AccountsReadProjection> GetAccountsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var accounts = await _context.Accounts
            .AsNoTracking()
            .Where(account =>
                account.UserId == userId &&
                !account.IsDeleted &&
                !account.IsSystemAccount)
            .OrderBy(account => account.Name)
            .Select(account => new AccountReadProjection(
                account.Id,
                account.Name,
                account.Type,
                account.CurrentBalance))
            .ToArrayAsync(cancellationToken);

        var totalAssets = accounts.Sum(account => account.Balance);

        return new AccountsReadProjection(totalAssets, accounts);
    }
}
