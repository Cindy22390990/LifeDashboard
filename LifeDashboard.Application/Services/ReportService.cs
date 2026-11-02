using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Application.Services.Interfaces;

namespace LifeDashboard.Application.Services;

public class ReportService : IReportService
{
    private readonly IReportRepository _reportRepository;

    public ReportService(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<MonthlyReportDto> GetMonthlyReportAsync(
        Guid userId,
        int year,
        int month)
    {
        if (year is < 1 or > 9999)
        {
            throw new ArgumentOutOfRangeException(nameof(year));
        }

        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month));
        }

        var startDate = new DateOnly(year, month, 1);
        DateOnly? endDateExclusive =
            year == 9999 && month == 12
                ? null
                : startDate.AddMonths(1);

        var totals = await _reportRepository.GetMonthlyTotalsAsync(
            userId,
            startDate,
            endDateExclusive);
        var categories = await _reportRepository.GetCategoryBreakdownAsync(
            userId,
            startDate,
            endDateExclusive);

        return new MonthlyReportDto
        {
            Year = year,
            Month = month,
            TotalIncome = totals.TotalIncome,
            TotalExpense = totals.TotalExpense,
            Balance = totals.TotalIncome - totals.TotalExpense,
            Categories = categories
        };
    }
}
