using LifeDashboard.Application.Models.ReadModels;
using LifeDashboard.Application.Services.Interfaces;

namespace LifeDashboard.Application.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IBudgetRadarService _budgetRadarService;
    private readonly IAccountService _accountService;
    private readonly IGoalService _goalService;

    public DashboardService(
        IBudgetRadarService budgetRadarService,
        IAccountService accountService,
        IGoalService goalService)
    {
        _budgetRadarService = budgetRadarService;
        _accountService = accountService;
        _goalService = goalService;
    }

    public async Task<DashboardSummaryReadModel> GetSummaryAsync(
        Guid userId,
        DateOnly currentDate,
        CancellationToken cancellationToken)
    {
        var budget = await _budgetRadarService.GetRadarStateAsync(userId, currentDate);
        var accounts = await _accountService.GetAllAccountsAsync(userId);
        var goals = await _goalService.GetAllAsync(userId, cancellationToken);

        return new DashboardSummaryReadModel(
            budget,
            Array.AsReadOnly(accounts.ToArray()),
            Array.AsReadOnly(goals.ToArray()));
    }
}