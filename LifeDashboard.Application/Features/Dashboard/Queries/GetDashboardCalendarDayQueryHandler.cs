using LifeDashboard.Application.Features.Dashboard.Dtos;
using LifeDashboard.Application.Features.Dashboard.Interfaces;
using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Application.Features.Dashboard.Queries;

public sealed class GetDashboardCalendarDayQueryHandler
{
    private readonly IDashboardCalendarReader _reader;

    public GetDashboardCalendarDayQueryHandler(IDashboardCalendarReader reader)
    {
        _reader = reader;
    }

    public async Task<DashboardDayDetailDto> HandleAsync(
        GetDashboardCalendarDayQuery query,
        CancellationToken cancellationToken)
    {
        var projection = await _reader.GetDayAsync(
            query.UserId,
            query.Date,
            cancellationToken);
        var transactions = projection.Transactions
            .Select(transaction => new DayTransactionItemDto(
                transaction.Id,
                transaction.Note,
                transaction.CategoryName,
                transaction.AccountName,
                transaction.Amount,
                ToContractType(transaction.Type),
                transaction.TransactionDate))
            .ToArray();

        return new DashboardDayDetailDto(
            projection.Date,
            projection.Transactions
                .Where(transaction => transaction.Type == TransactionType.Income)
                .Sum(transaction => transaction.Amount),
            projection.Transactions
                .Where(transaction => transaction.Type == TransactionType.Expense)
                .Sum(transaction => transaction.Amount),
            transactions);
    }

    private static string ToContractType(TransactionType type) =>
        type switch
        {
            TransactionType.Expense => "Expense",
            TransactionType.Income => "Income",
            TransactionType.Transfer => "Transfer",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
}
