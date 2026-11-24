using LifeDashboard.Application.Features.Dashboard.Interfaces;
using LifeDashboard.Application.Features.Dashboard.Models;
using LifeDashboard.Domain.Enums;
using LifeDashboard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LifeDashboard.Infrastructure.Repositories;

public sealed class DashboardCalendarReader : IDashboardCalendarReader
{
    private readonly LifeDashboardDbContext _context;

    public DashboardCalendarReader(LifeDashboardDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardCalendarDayProjection> GetDayAsync(
        Guid userId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var transactions = await _context.Transactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.UserId == userId &&
                !transaction.IsDeleted &&
                transaction.TransactionDate == date)
            .OrderByDescending(transaction => transaction.CreatedAtUtc)
            .Select(transaction => new CalendarTransactionProjection(
                transaction.Id,
                transaction.Note,
                transaction.CategoryEntity != null
                    ? transaction.CategoryEntity.Name
                    : transaction.Category,
                transaction.Account.Name,
                transaction.Amount,
                transaction.Type,
                transaction.TransactionDate))
            .ToArrayAsync(cancellationToken);

        return new DashboardCalendarDayProjection(date, transactions);
    }

    public async Task<DashboardCalendarMonthProjection> GetMonthAsync(
        Guid userId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        var monthStart = new DateOnly(year, month, 1);
        var query = _context.Transactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.UserId == userId &&
                !transaction.IsDeleted &&
                transaction.TransactionDate >= monthStart);

        if (year != 9999 || month != 12)
        {
            query = query.Where(transaction =>
                transaction.TransactionDate < monthStart.AddMonths(1));
        }

        var typeTotals = await query
            .GroupBy(transaction => new
            {
                transaction.TransactionDate,
                transaction.Type
            })
            .Select(group => new
            {
                group.Key.TransactionDate,
                group.Key.Type,
                Total = group.Sum(transaction => transaction.Amount)
            })
            .ToArrayAsync(cancellationToken);
        var days = typeTotals
            .GroupBy(total => total.TransactionDate)
            .OrderBy(group => group.Key)
            .Select(group => new CalendarDaySummaryProjection(
                group.Key,
                group.Where(total => total.Type == TransactionType.Income)
                    .Sum(total => total.Total),
                group.Where(total => total.Type == TransactionType.Expense)
                    .Sum(total => total.Total),
                group.Any(total => total.Type == TransactionType.Transfer)))
            .ToArray();

        return new DashboardCalendarMonthProjection(year, month, days);
    }
}
