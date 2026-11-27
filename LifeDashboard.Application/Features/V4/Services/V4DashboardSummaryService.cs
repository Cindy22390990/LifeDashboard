using LifeDashboard.Application.Features.V4.Dtos;
using LifeDashboard.Application.Features.V4.Interfaces;
using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Application.Features.V4.Services;

public sealed class V4DashboardSummaryService : IV4DashboardSummaryService
{
    private readonly IV4AccountsReader _accountsReader;
    private readonly IV4TransactionReader _transactionReader;

    public V4DashboardSummaryService(
        IV4AccountsReader accountsReader,
        IV4TransactionReader transactionReader)
    {
        _accountsReader = accountsReader;
        _transactionReader = transactionReader;
    }

    public async Task<DashboardSummaryReadDto> GetSummaryAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var accounts = await _accountsReader.GetAccountsAsync(
            userId,
            cancellationToken);

        var monthlyIncome = await _transactionReader.GetTransactionsAsync(
            userId,
            CreateMonthlyQuery(today, TransactionType.Income),
            cancellationToken);

        var monthlyExpense = await _transactionReader.GetTransactionsAsync(
            userId,
            CreateMonthlyQuery(today, TransactionType.Expense),
            cancellationToken);

        return new DashboardSummaryReadDto(
            accounts.TotalAssets,
            monthlyIncome.TotalIncome,
            monthlyExpense.TotalExpense);
    }

    private static V4TransactionQuery CreateMonthlyQuery(
        DateOnly businessDate,
        TransactionType transactionType)
    {
        return new V4TransactionQuery(
            businessDate.Year,
            businessDate.Month,
            Date: null,
            StartDate: null,
            EndDate: null,
            Keyword: null,
            AccountId: null,
            CategoryId: null,
            TransactionType: transactionType,
            SortBy: null,
            SortDirection: "desc",
            Page: 1,
            PageSize: 1);
    }
}
