using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Application.Features.Dashboard.Dtos;

public sealed record DashboardAccountDto(
    Guid Id,
    string Name,
    AccountType Type,
    string AccountType,
    decimal Balance);
