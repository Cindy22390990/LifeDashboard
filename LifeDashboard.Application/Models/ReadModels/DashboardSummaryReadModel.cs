using LifeDashboard.Application.Models.DTOs;

namespace LifeDashboard.Application.Models.ReadModels;

public sealed record DashboardSummaryReadModel(
    RadarStateDto Budget,
    IReadOnlyList<AccountDto> Accounts,
    IReadOnlyList<GoalDto> Goals);