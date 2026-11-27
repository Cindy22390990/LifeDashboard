using LifeDashboard.Application.Features.V4.Dtos;
using LifeDashboard.Application.Features.V4.Interfaces;
using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Application.Features.V4.Services;

public sealed class V4AccountsService : IV4AccountsService
{
    private readonly IV4AccountsReader _reader;

    public V4AccountsService(IV4AccountsReader reader)
    {
        _reader = reader;
    }

    public async Task<AccountsReadDto> GetAccountsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var projection = await _reader.GetAccountsAsync(
            userId,
            cancellationToken);

        var items = projection.Items
            .Select(account => new AccountReadItemDto(
                account.Id,
                account.Name,
                account.AccountType switch
                {
                    AccountType.Cashflow => "Cashflow",
                    AccountType.Saving => "Saving",
                    AccountType.Investment => "Invest",
                    _ => "Unknown"
                },
                account.Balance))
            .ToArray();

        return new AccountsReadDto(projection.TotalAssets, items);
    }
}
