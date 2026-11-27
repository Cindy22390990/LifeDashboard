namespace LifeDashboard.Application.Features.V4.Dtos;

public sealed record AccountsReadDto(
    decimal TotalAssets,
    IReadOnlyList<AccountReadItemDto> Items);

public sealed record AccountReadItemDto(
    Guid Id,
    string Name,
    string AccountType,
    decimal Balance);
