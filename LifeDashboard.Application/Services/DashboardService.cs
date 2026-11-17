using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Models.ReadModels;
using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Application.Services.Interfaces;

namespace LifeDashboard.Application.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IBudgetRadarService _budgetRadarService;
    private readonly IAccountService _accountService;
    private readonly IGoalService _goalService;
    private readonly IReportRepository _reportRepository;
    private readonly ITransactionService _transactionService;

    public DashboardService(
        IBudgetRadarService budgetRadarService,
        IAccountService accountService,
        IGoalService goalService,
        IReportRepository reportRepository,
        ITransactionService transactionService)
    {
        _budgetRadarService = budgetRadarService;
        _accountService = accountService;
        _goalService = goalService;
        _reportRepository = reportRepository;
        _transactionService = transactionService;
    }

    public async Task<DashboardSummaryReadModel> GetSummaryAsync(
        Guid userId,
        DateOnly currentDate,
        CancellationToken cancellationToken)
    {
        var budget = await _budgetRadarService.GetRadarStateAsync(userId, currentDate);
        var accounts = await _accountService.GetAllAccountsAsync(userId);
        var goals = await _goalService.GetAllAsync(userId, cancellationToken);
        var accountList = accounts.ToArray();
        var totalAssets = accountList
            .Where(account => !account.IsSystemAccount)
            .Sum(account => account.CurrentBalance);

        var monthStart = new DateOnly(currentDate.Year, currentDate.Month, 1);
        DateOnly? monthEndExclusive =
            currentDate.Year == 9999 && currentDate.Month == 12
                ? null
                : monthStart.AddMonths(1);
        var monthlyTotals = await _reportRepository.GetMonthlyTotalsAsync(
            userId,
            monthStart,
            monthEndExclusive);
        var recentTransactions = (await _transactionService.GetAllTransactionsAsync(userId))
            .Take(5)
            .ToArray();

        return new DashboardSummaryReadModel(
            budget,
            Array.AsReadOnly(accountList),
            Array.AsReadOnly(goals.ToArray()),
            totalAssets,
            monthlyTotals.TotalIncome,
            monthlyTotals.TotalExpense,
            Array.AsReadOnly(recentTransactions));
    }
}