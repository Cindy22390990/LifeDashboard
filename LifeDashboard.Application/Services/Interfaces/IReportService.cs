using LifeDashboard.Application.Models.DTOs;

namespace LifeDashboard.Application.Services.Interfaces;

public interface IReportService
{
    Task<MonthlyReportDto> GetMonthlyReportAsync(
        Guid userId,
        int year,
        int month);
}
