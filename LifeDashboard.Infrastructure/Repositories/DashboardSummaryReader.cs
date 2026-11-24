using LifeDashboard.Application.Features.Dashboard.Interfaces;
using LifeDashboard.Application.Features.Dashboard.Models;
using LifeDashboard.Domain.Enums;
using LifeDashboard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LifeDashboard.Infrastructure.Repositories;

public sealed class DashboardSummaryReader : IDashboardSummaryReader
{
    private readonly LifeDashboardDbContext _context;

    public DashboardSummaryReader(LifeDashboardDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummaryProjection> GetSummaryAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var businessDate = DateOnly.FromDateTime(DateTime.UtcNow);

        var totalAssets = await _context.Accounts
            .AsNoTracking()
            .Where(account =>
                account.UserId == userId &&
                !account.IsDeleted &&
                !account.IsSystemAccount)
            .SumAsync(account => (decimal?)account.CurrentBalance, cancellationToken)
            ?? 0m;

        var todayExpense = await _context.Transactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.UserId == userId &&
                !transaction.IsDeleted &&
                transaction.TransactionDate == businessDate &&
                transaction.Type == TransactionType.Expense)
            .SumAsync(transaction => (decimal?)transaction.Amount, cancellationToken)
            ?? 0m;

        var accounts = await _context.Accounts
            .AsNoTracking()
            .Where(account =>
                account.UserId == userId &&
                !account.IsDeleted &&
                !account.IsSystemAccount)
            .OrderBy(account => account.Name)
            .Select(account => new DashboardAccountProjection(
                account.Id,
                account.Name,
                account.Type,
                account.CurrentBalance))
            .ToArrayAsync(cancellationToken);

        var recentTransactions = await _context.Transactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.UserId == userId &&
                !transaction.IsDeleted)
            .OrderByDescending(transaction => transaction.TransactionDate)
            .ThenByDescending(transaction => transaction.CreatedAtUtc)
            .Take(5)
            .Select(transaction => new RecentTransactionProjection(
                transaction.Id,
                transaction.Note,
                transaction.CategoryEntity != null
                    ? transaction.CategoryEntity.Name
                    : string.Empty,
                transaction.Account.Name,
                transaction.Amount,
                transaction.Type,
                transaction.TransactionDate))
            .ToArrayAsync(cancellationToken);

        return new DashboardSummaryProjection(
            totalAssets,
            todayExpense,
            businessDate,
            accounts,
            recentTransactions);
    }
}
