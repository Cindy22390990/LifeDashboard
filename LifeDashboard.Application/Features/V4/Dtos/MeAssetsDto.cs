namespace LifeDashboard.Application.Features.V4.Dtos;

public sealed record MeAssetsDto(
    decimal TotalAssets,
    IReadOnlyList<MeAccountItemDto> Accounts);

public sealed record MeAccountItemDto(
    Guid Id,
    string Name,
    string AccountType,
    decimal Balance);
