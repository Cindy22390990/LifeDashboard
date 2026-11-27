namespace LifeDashboard.Application.Features.V4.Models;

public sealed record AccountsReadProjection(
    decimal TotalAssets,
    IReadOnlyList<AccountReadProjection> Items);

public sealed record AccountReadProjection(
    Guid Id,
    string Name,
    LifeDashboard.Domain.Enums.AccountType AccountType,
    decimal Balance);
