using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Application.Services.Interfaces;
using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Application.Services;

public class BudgetRadarService : IBudgetRadarService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IBudgetRadarQueryRepository _budgetRadarQueryRepository;
    private readonly IFixedCommitmentRepository _fixedCommitmentRepository;
    private readonly ISettingService _settingService;
    private readonly BudgetRadarAssembler _budgetRadarAssembler;

    public BudgetRadarService(
        ITransactionRepository transactionRepository,
        IBudgetRadarQueryRepository budgetRadarQueryRepository,
        IFixedCommitmentRepository fixedCommitmentRepository,
        ISettingService settingService,
        BudgetRadarAssembler budgetRadarAssembler)
    {
        _transactionRepository = transactionRepository;
        _budgetRadarQueryRepository = budgetRadarQueryRepository;
        _fixedCommitmentRepository = fixedCommitmentRepository;
        _settingService = settingService;
        _budgetRadarAssembler = budgetRadarAssembler;
    }

    public async Task<RadarStateDto> GetRadarStateAsync(
        Guid userId,
        DateOnly currentDate)
    {
        var setting = await _settingService.GetSettingAsync(userId);
        var period = GetBudgetPeriod(currentDate, setting.Payday);

        var income = await _transactionRepository.GetTotalAmountAsync(
            userId,
            period.StartDate,
            period.EndDate,
            TransactionType.Income);

        var expenseSummary = await _budgetRadarQueryRepository.GetExpenseSummaryAsync(
            userId,
            period.StartDate,
            period.EndDate,
            currentDate);

        var goalPurchaseAggregate = await _budgetRadarQueryRepository.GetGoalPurchaseAggregateAsync(
            userId,
            period.StartDate,
            period.EndDate);

        var commitments =
            await _fixedCommitmentRepository.GetActiveForPayrollPeriodAsync(
                userId,
                period.PayrollPeriodMonth);

        var readModel = _budgetRadarAssembler.Assemble(
            currentDate,
            period.StartDate,
            period.EndDate,
            income,
            expenseSummary,
            goalPurchaseAggregate,
            commitments);

        return _budgetRadarAssembler.ToRadarStateDto(readModel);
    }

    private static BudgetPeriod GetBudgetPeriod(DateOnly currentDate, int payday)
    {
        var currentMonthPayday = GetPaydayDate(currentDate.Year, currentDate.Month, payday);

        var startDate = currentDate >= currentMonthPayday
            ? currentMonthPayday
            : GetPaydayDate(
                currentDate.AddMonths(-1).Year,
                currentDate.AddMonths(-1).Month,
                payday);

        var nextMonth = startDate.AddMonths(1);
        var nextPayday = GetPaydayDate(nextMonth.Year, nextMonth.Month, payday);

        return new BudgetPeriod(
            startDate,
            nextPayday.AddDays(-1),
            new DateOnly(startDate.Year, startDate.Month, 1));
    }

    private static DateOnly GetPaydayDate(int year, int month, int payday)
    {
        var day = Math.Min(payday, DateTime.DaysInMonth(year, month));
        return new DateOnly(year, month, day);
    }

    private sealed record BudgetPeriod(
        DateOnly StartDate,
        DateOnly EndDate,
        DateOnly PayrollPeriodMonth);
}
