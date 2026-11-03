using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Application.Services.Interfaces;
using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Application.Services;

public class BudgetRadarService : IBudgetRadarService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IFixedCommitmentRepository _fixedCommitmentRepository;
    private readonly ISettingService _settingService;

    public BudgetRadarService(
        ITransactionRepository transactionRepository,
        IFixedCommitmentRepository fixedCommitmentRepository,
        ISettingService settingService)
    {
        _transactionRepository = transactionRepository;
        _fixedCommitmentRepository = fixedCommitmentRepository;
        _settingService = settingService;
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

        var livingExpenses = await _transactionRepository.GetTotalAmountAsync(
            userId,
            period.StartDate,
            period.EndDate,
            TransactionType.Expense,
            BucketType.Living);

        var todaySpent = await _transactionRepository.GetTotalAmountAsync(
            userId,
            currentDate,
            currentDate,
            TransactionType.Expense,
            BucketType.Living);

        var uncategorizedExpenseAmount =
            await _transactionRepository.GetUncategorizedExpenseTotalAsync(
                userId,
                period.StartDate,
                period.EndDate);

        var commitments =
            await _fixedCommitmentRepository.GetActiveForPayrollPeriodAsync(
                userId,
                period.PayrollPeriodMonth);

        var totalLivingBudget = income - commitments.Sum(commitment => commitment.MonthlyAmount);
        var remainingLivingBudget = totalLivingBudget - livingExpenses;
        var remainingDays = period.EndDate.DayNumber - currentDate.DayNumber + 1;
        var dailySafeLimit = decimal.Round(
            remainingLivingBudget / remainingDays,
            2,
            MidpointRounding.AwayFromZero);

        return new RadarStateDto
        {
            DailySafeLimit = dailySafeLimit,
            TodaySpent = todaySpent,
            TodayRemaining = dailySafeLimit - todaySpent,
            RemainingDays = remainingDays,
            TotalLivingBudget = totalLivingBudget,
            UncategorizedExpenseAmount = uncategorizedExpenseAmount
        };
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
