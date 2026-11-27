using LifeDashboard.Application.Features.V4.Interfaces;
using LifeDashboard.Application.Features.V4.Models;
using LifeDashboard.Infrastructure.Data;
using LifeDashboard.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LifeDashboard.Infrastructure.Repositories;

public sealed class V4HomeReader : IV4HomeReader
{
    private readonly LifeDashboardDbContext _context;

    public V4HomeReader(LifeDashboardDbContext context)
    {
        _context = context;
    }

    public async Task<V4HomeProjection> GetHomeProjectionAsync(
        Guid userId,
        DateOnly businessDate,
        CancellationToken cancellationToken)
    {
        var monthStart = new DateOnly(businessDate.Year, businessDate.Month, 1);
        var daysInMonth = DateTime.DaysInMonth(businessDate.Year, businessDate.Month);
        var monthEnd = new DateOnly(
            businessDate.Year,
            businessDate.Month,
            daysInMonth);
        var yesterday = businessDate == monthStart
            ? monthStart
            : businessDate.AddDays(-1);
        var accumulatedStart = businessDate == monthStart
            ? monthStart
            : monthStart;

        var setting = await _context.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.UserId == userId,
                cancellationToken);

        var monthlyIncome = await _context.Transactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.UserId == userId &&
                transaction.TransactionDate >= monthStart &&
                transaction.TransactionDate <= monthEnd &&
                transaction.Type == TransactionType.Income)
            .SumAsync(transaction => (decimal?)transaction.Amount, cancellationToken)
            ?? 0m;

        var availableMonthlyBudget = setting == null
            ? 0m
            : monthlyIncome * setting.Allocation.Living / 100m;

        var reservedFutureCommitments = await _context.FixedCommitments
            .AsNoTracking()
            .Where(commitment =>
                commitment.UserId == userId &&
                commitment.StartMonth <= monthStart &&
                (!commitment.EndMonth.HasValue ||
                 commitment.EndMonth.Value >= monthStart))
            .SumAsync(commitment => (decimal?)commitment.MonthlyAmount, cancellationToken)
            ?? 0m;

        var accumulatedExpenseThroughYesterday = await _context.Transactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.UserId == userId &&
                transaction.TransactionDate >= accumulatedStart &&
                transaction.TransactionDate <= yesterday &&
                transaction.Type == TransactionType.Expense)
            .SumAsync(transaction => (decimal?)transaction.Amount, cancellationToken)
            ?? 0m;

        var dailySpent = await _context.Transactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.UserId == userId &&
                transaction.TransactionDate == businessDate &&
                transaction.Type == TransactionType.Expense)
            .SumAsync(transaction => (decimal?)transaction.Amount, cancellationToken)
            ?? 0m;

        var todayTransactions = await _context.Transactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.UserId == userId &&
                transaction.TransactionDate == businessDate)
            .OrderByDescending(transaction => transaction.CreatedAtUtc)
            .Select(transaction => new V4HomeTransactionProjection(
                transaction.Id,
                transaction.Note,
                transaction.Amount,
                transaction.Type,
                transaction.TransactionDate))
            .ToArrayAsync(cancellationToken);

        var savedAmount = await _context.Transactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.UserId == userId &&
                transaction.Type == TransactionType.Transfer &&
                transaction.TransactionDate >= monthStart &&
                transaction.TransactionDate <= monthEnd &&
                transaction.ToAccount != null &&
                transaction.ToAccount.Type == AccountType.Saving)
            .SumAsync(transaction => (decimal?)transaction.Amount, cancellationToken)
            ?? 0m;

        var investedAmount = await _context.Transactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.UserId == userId &&
                transaction.Type == TransactionType.Transfer &&
                transaction.TransactionDate >= monthStart &&
                transaction.TransactionDate <= monthEnd &&
                transaction.ToAccount != null &&
                transaction.ToAccount.Type == AccountType.Investment)
            .SumAsync(transaction => (decimal?)transaction.Amount, cancellationToken)
            ?? 0m;

        var elapsedDays = businessDate.Day - 1;
        var remainingDays = monthEnd.DayNumber - businessDate.DayNumber + 1;

        return new V4HomeProjection(
            AvailableMonthlyBudget: availableMonthlyBudget,
            ReservedFutureCommitments: reservedFutureCommitments,
            AccumulatedExpenseThroughYesterday: accumulatedExpenseThroughYesterday,
            DailySpent: dailySpent,
            DaysInMonth: daysInMonth,
            ElapsedDays: elapsedDays,
            RemainingDays: remainingDays,
            MonthlyProgress: new V4MonthlyFinancialProgressProjection(
                savedAmount,
                investedAmount,
                "AccountTransfer"),
            TodayTransactions: todayTransactions);
    }
}
