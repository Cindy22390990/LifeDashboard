using LifeDashboard.Application.Features.Dashboard.Models;

namespace LifeDashboard.Application.Features.Dashboard.Interfaces;

public interface IDashboardCalendarReader
{
    Task<DashboardCalendarDayProjection> GetDayAsync(
        Guid userId,
        DateOnly date,
        CancellationToken cancellationToken);

    Task<DashboardCalendarMonthProjection> GetMonthAsync(
        Guid userId,
        int year,
        int month,
        CancellationToken cancellationToken);
}
