using LifeDashboard.Application.Features.V4.Dtos;
using LifeDashboard.Application.Features.V4.Interfaces;

namespace LifeDashboard.Application.Features.V4.Services;

public sealed class V4MeAssetsService : IV4MeAssetsService
{
    private readonly IV4MeAssetsReader _reader;

    public V4MeAssetsService(IV4MeAssetsReader reader)
    {
        _reader = reader;
    }

    public async Task<MeAssetsDto> GetMeAssetsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var projection = await _reader.GetMeAssetsProjectionAsync(
            userId,
            cancellationToken);

        var accounts = projection.Accounts
            .Select(account => new MeAccountItemDto(
                account.Id,
                account.Name,
                account.AccountType.ToString(),
                account.Balance))
            .ToArray();

        return new MeAssetsDto(projection.TotalAssets, accounts);
    }
}
