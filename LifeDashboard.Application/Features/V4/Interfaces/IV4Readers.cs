using LifeDashboard.Application.Features.V4.Models;
using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Application.Features.V4.Interfaces;

public interface IV4HomeReader
{
    Task<V4HomeProjection> GetHomeProjectionAsync(
        Guid userId,
        DateOnly businessDate,
        CancellationToken cancellationToken);
}

public interface IV4MeAssetsReader
{
    Task<MeAssetsProjection> GetMeAssetsProjectionAsync(
        Guid userId,
        CancellationToken cancellationToken);
}

public interface IV4AccountsReader
{
    Task<AccountsReadProjection> GetAccountsAsync(
        Guid userId,
        CancellationToken cancellationToken);
}

public sealed record V4TransactionQuery(
    int? Year,
    int? Month,
    DateOnly? Date,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? Keyword,
    Guid? AccountId,
    Guid? CategoryId,
    TransactionType? TransactionType,
    string? SortBy,
    string SortDirection,
    int Page,
    int PageSize);

public interface IV4TransactionReader
{
    Task<V4TransactionListProjection> GetTransactionsAsync(
        Guid userId,
        V4TransactionQuery query,
        CancellationToken cancellationToken);
}

public interface IV4CategoryReader
{
    Task<IReadOnlyList<CategoryReadProjection>> GetCategoriesAsync(
        Guid userId,
        CancellationToken cancellationToken);
}
