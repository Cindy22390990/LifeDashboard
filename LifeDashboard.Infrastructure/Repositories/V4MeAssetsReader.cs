using LifeDashboard.Application.Features.V4.Interfaces;
using LifeDashboard.Application.Features.V4.Models;
using LifeDashboard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LifeDashboard.Infrastructure.Repositories;

public sealed class V4MeAssetsReader : IV4MeAssetsReader
{
    private readonly LifeDashboardDbContext _context;

    public V4MeAssetsReader(LifeDashboardDbContext context)
    {
        _context = context;
    }

    public async Task<MeAssetsProjection> GetMeAssetsProjectionAsync(
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
            .Select(account => new MeAccountProjection(
                account.Id,
                account.Name,
                account.Type,
                account.CurrentBalance))
            .ToArrayAsync(cancellationToken);

        var totalAssets = accounts.Sum(account => account.Balance);
        return new MeAssetsProjection(totalAssets, accounts);
    }
}
