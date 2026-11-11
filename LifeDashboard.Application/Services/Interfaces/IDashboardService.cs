using LifeDashboard.Application.Models.ReadModels;

namespace LifeDashboard.Application.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryReadModel> GetSummaryAsync(
        Guid userId,
        DateOnly currentDate,
        CancellationToken cancellationToken);
}