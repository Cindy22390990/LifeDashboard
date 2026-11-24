using LifeDashboard.Application.Features.Dashboard.Dtos;
using LifeDashboard.Application.Features.Dashboard.Interfaces;

namespace LifeDashboard.Application.Features.Dashboard.Queries;

public sealed class GetDashboardCalendarMonthQueryHandler
{
    private readonly IDashboardCalendarReader _reader;

    public GetDashboardCalendarMonthQueryHandler(IDashboardCalendarReader reader)
    {
        _reader = reader;
    }

    public async Task<DashboardCalendarMonthDto> HandleAsync(
        GetDashboardCalendarMonthQuery query,
        CancellationToken cancellationToken)
    {
        var projection = await _reader.GetMonthAsync(
            query.UserId,
            query.Year,
            query.Month,
            cancellationToken);

        return new DashboardCalendarMonthDto(
            projection.Year,
            projection.Month,
            projection.Days
                .Select(day => new CalendarDaySummaryDto(
                    day.Date,
                    day.TotalIncome,
                    day.TotalExpense,
                    day.HasTransfer))
                .ToArray());
    }
}
