using LifeDashboard.Application.Features.Dashboard.Models;

namespace LifeDashboard.Application.Features.Dashboard.Interfaces;

public interface IDashboardSummaryReader
{
    Task<DashboardSummaryProjection> GetSummaryAsync(
        Guid userId,
        CancellationToken cancellationToken);
}
