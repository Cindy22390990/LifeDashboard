namespace LifeDashboard.Application.Models.DTOs;

public sealed class DashboardSummaryDto
{
    public required RadarStateDto Budget { get; init; }
    public required IReadOnlyList<AccountDto> Accounts { get; init; }
    public required IReadOnlyList<GoalDto> Goals { get; init; }
}