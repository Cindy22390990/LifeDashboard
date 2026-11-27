namespace LifeDashboard.Application.Features.V4.Models;

public sealed record MeAssetsProjection(
    decimal TotalAssets,
    IReadOnlyList<MeAccountProjection> Accounts);

public sealed record MeAccountProjection(
    Guid Id,
    string Name,
    LifeDashboard.Domain.Enums.AccountType AccountType,
    decimal Balance);
