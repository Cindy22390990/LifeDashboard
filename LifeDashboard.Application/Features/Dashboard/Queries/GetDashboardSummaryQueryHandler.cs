using LifeDashboard.Application.Features.Dashboard.Dtos;
using LifeDashboard.Application.Features.Dashboard.Interfaces;
using LifeDashboard.Application.Features.Dashboard.Models;

namespace LifeDashboard.Application.Features.Dashboard.Queries;

public sealed class GetDashboardSummaryQueryHandler
{
    private readonly IDashboardSummaryReader _reader;

    public GetDashboardSummaryQueryHandler(IDashboardSummaryReader reader)
    {
        _reader = reader;
    }

    public async Task<DashboardSummaryDto> HandleAsync(
        GetDashboardSummaryQuery query,
        CancellationToken cancellationToken)
    {
        var projection = await _reader.GetSummaryAsync(
            query.UserId,
            cancellationToken);

        return Map(projection);
    }

    private static DashboardSummaryDto Map(DashboardSummaryProjection projection)
    {
        var accounts = projection.Accounts
            .Select(account => new DashboardAccountDto(
                account.Id,
                account.Name,
                account.Type,
                account.Balance))
            .ToArray();

        var recentTransactions = projection.RecentTransactions
            .Select(transaction => new RecentTransactionDto(
                transaction.Id,
                transaction.Note,
                transaction.CategoryName,
                transaction.AccountName,
                transaction.Amount,
                transaction.Type,
                transaction.TransactionDate))
            .ToArray();

        return new DashboardSummaryDto(
            projection.TotalAssets,
            projection.TodayExpense,
            projection.BusinessDate,
            accounts,
            recentTransactions);
    }
}
