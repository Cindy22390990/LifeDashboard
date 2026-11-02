using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Domain.Enums;
using LifeDashboard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LifeDashboard.Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly LifeDashboardDbContext _context;

    public ReportRepository(LifeDashboardDbContext context)
    {
        _context = context;
    }

    public async Task<(decimal TotalIncome, decimal TotalExpense)> GetMonthlyTotalsAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly? endDateExclusive)
    {
        var transactions = GetReportTransactions(userId, startDate, endDateExclusive);
        var totals = await transactions
            .GroupBy(_ => 1)
            .Select(group => new
            {
                TotalIncome = group
                    .Where(transaction => transaction.Type == TransactionType.Income)
                    .Sum(transaction => (decimal?)transaction.Amount) ?? 0m,
                TotalExpense = group
                    .Where(transaction => transaction.Type == TransactionType.Expense)
                    .Sum(transaction => (decimal?)transaction.Amount) ?? 0m
            })
            .SingleOrDefaultAsync();

        return (totals?.TotalIncome ?? 0m, totals?.TotalExpense ?? 0m);
    }

    public async Task<List<CategoryBreakdownDto>> GetCategoryBreakdownAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly? endDateExclusive)
    {
        var transactions = GetReportTransactions(userId, startDate, endDateExclusive);
        var userCategories = _context.Categories
            .AsNoTracking()
            .Where(category => category.UserId == userId);

        var categorizedTransactions =
            from transaction in transactions
            join category in userCategories
                on transaction.CategoryId equals (Guid?)category.Id into categoryJoin
            from category in categoryJoin.DefaultIfEmpty()
            select new
            {
                CategoryId = category == null ? (Guid?)null : transaction.CategoryId,
                CategoryName = category != null
                    ? category.Name
                    : transaction.Category == null || transaction.Category.Trim() == ""
                        ? "未分類"
                        : transaction.Category,
                BucketType = category == null
                    ? (BucketType?)null
                    : category.BucketType,
                transaction.Amount,
                transaction.Type
            };

        return await categorizedTransactions
            .GroupBy(transaction => new
            {
                transaction.CategoryId,
                transaction.CategoryName,
                transaction.BucketType
            })
            .Select(group => new CategoryBreakdownDto
            {
                CategoryId = group.Key.CategoryId,
                CategoryName = group.Key.CategoryName,
                BucketType = group.Key.BucketType,
                Income = group
                    .Where(transaction => transaction.Type == TransactionType.Income)
                    .Sum(transaction => (decimal?)transaction.Amount) ?? 0m,
                Expense = group
                    .Where(transaction => transaction.Type == TransactionType.Expense)
                    .Sum(transaction => (decimal?)transaction.Amount) ?? 0m,
                TransactionCount = group.Count()
            })
            .OrderBy(category => category.CategoryName)
            .ToListAsync();
    }

    private IQueryable<Domain.Entities.Transaction> GetReportTransactions(
        Guid userId,
        DateOnly startDate,
        DateOnly? endDateExclusive)
    {
        var query = _context.Transactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.UserId == userId &&
                transaction.TransactionDate >= startDate &&
                (transaction.Type == TransactionType.Income ||
                 transaction.Type == TransactionType.Expense));

        if (endDateExclusive.HasValue)
        {
            query = query.Where(
                transaction => transaction.TransactionDate < endDateExclusive.Value);
        }

        return query;
    }
}
