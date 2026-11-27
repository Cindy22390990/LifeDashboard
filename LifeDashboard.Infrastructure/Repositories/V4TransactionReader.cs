using LifeDashboard.Application.Features.V4.Interfaces;
using LifeDashboard.Application.Features.V4.Models;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Domain.Enums;
using LifeDashboard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LifeDashboard.Infrastructure.Repositories;

public sealed class V4TransactionReader : IV4TransactionReader
{
    private readonly LifeDashboardDbContext _context;

    public V4TransactionReader(LifeDashboardDbContext context)
    {
        _context = context;
    }

    public async Task<V4TransactionListProjection> GetTransactionsAsync(
        Guid userId,
        V4TransactionQuery query,
        CancellationToken cancellationToken)
    {
        var transactions = _context.Transactions
            .AsNoTracking()
            .Include(transaction => transaction.Account)
            .Include(transaction => transaction.CategoryEntity)
            .Where(transaction => transaction.UserId == userId);

        if (query.Date.HasValue)
        {
            transactions = transactions.Where(transaction =>
                transaction.TransactionDate == query.Date.Value);
        }
        else if (query.Year.HasValue && query.Month.HasValue)
        {
            var monthStart = new DateOnly(query.Year.Value, query.Month.Value, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            transactions = transactions.Where(transaction =>
                transaction.TransactionDate >= monthStart &&
                transaction.TransactionDate <= monthEnd);
        }

        if (query.StartDate.HasValue)
        {
            transactions = transactions.Where(transaction =>
                transaction.TransactionDate >= query.StartDate.Value);
        }

        if (query.EndDate.HasValue)
        {
            transactions = transactions.Where(transaction =>
                transaction.TransactionDate <= query.EndDate.Value);
        }

        if (query.AccountId.HasValue)
        {
            transactions = transactions.Where(transaction =>
                transaction.AccountId == query.AccountId.Value);
        }

        if (query.CategoryId.HasValue)
        {
            transactions = transactions.Where(transaction =>
                transaction.CategoryId == query.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            transactions = transactions.Where(transaction =>
                transaction.Note.Contains(keyword));
        }

        if (query.TransactionType.HasValue)
        {
            transactions = transactions.Where(transaction =>
                transaction.Type == query.TransactionType.Value);
        }

        var totalIncome = await transactions
            .Where(transaction => transaction.Type == TransactionType.Income)
            .SumAsync(transaction => (decimal?)transaction.Amount, cancellationToken)
            ?? 0m;
        var totalExpense = await transactions
            .Where(transaction => transaction.Type == TransactionType.Expense)
            .SumAsync(transaction => (decimal?)transaction.Amount, cancellationToken)
            ?? 0m;

        var orderedTransactions = ApplySorting(transactions, query);
        var items = await orderedTransactions
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(transaction => new V4TransactionProjection(
                transaction.Id,
                transaction.Note,
                transaction.Amount,
                transaction.Type,
                transaction.TransactionDate,
                transaction.AccountId,
                transaction.Account.Name,
                transaction.CategoryId,
                transaction.CategoryEntity != null
                    ? transaction.CategoryEntity.Name
                    : null))
            .ToArrayAsync(cancellationToken);

        return new V4TransactionListProjection(
            totalIncome,
            totalExpense,
            items);
    }

    private static IOrderedQueryable<Transaction> ApplySorting(
        IQueryable<Transaction> transactions,
        V4TransactionQuery query)
    {
        var ascending = query.SortDirection == "asc";

        IOrderedQueryable<Transaction> orderedTransactions =
            query.SortBy switch
            {
                "amount" => ascending
                    ? transactions.OrderBy(transaction => transaction.Amount)
                    : transactions.OrderByDescending(transaction => transaction.Amount),
                "note" => ascending
                    ? transactions.OrderBy(transaction => transaction.Note)
                    : transactions.OrderByDescending(transaction => transaction.Note),
                _ => ascending
                    ? transactions.OrderBy(transaction => transaction.TransactionDate)
                    : transactions.OrderByDescending(transaction => transaction.TransactionDate)
            };

        if (query.SortBy is null or "transactiondate")
        {
            return orderedTransactions
                .ThenByDescending(transaction => transaction.CreatedAtUtc)
                .ThenByDescending(transaction => transaction.Id);
        }

        return orderedTransactions
            .ThenByDescending(transaction => transaction.TransactionDate)
            .ThenByDescending(transaction => transaction.CreatedAtUtc)
            .ThenByDescending(transaction => transaction.Id);
    }
}
