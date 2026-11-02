using LifeDashboard.Application.Models.DTOs;

namespace LifeDashboard.Application.Repositories.Interfaces;

public interface IReportRepository
{
    Task<(decimal TotalIncome, decimal TotalExpense)> GetMonthlyTotalsAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly? endDateExclusive);

    Task<List<CategoryBreakdownDto>> GetCategoryBreakdownAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly? endDateExclusive);
}
