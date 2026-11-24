using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Application.Features.Dashboard.Models;

public sealed record DashboardAccountProjection(
    Guid Id,
    string Name,
    AccountType Type,
    decimal Balance);
