using LifeDashboard.Application.Features.V4.Dtos;
using LifeDashboard.Application.Features.V4.Interfaces;
using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Domain.Entities;

namespace LifeDashboard.Application.Features.V4.Services;

public interface IV4HomeService
{
    Task<DashboardV4HomeDto> GetHomeAsync(
        Guid userId,
        DateOnly businessDate,
        CancellationToken cancellationToken);
}

public interface IV4MeAssetsService
{
    Task<MeAssetsDto> GetMeAssetsAsync(
        Guid userId,
        CancellationToken cancellationToken);
}

public interface IV4AccountsService
{
    Task<AccountsReadDto> GetAccountsAsync(
        Guid userId,
        CancellationToken cancellationToken);
}

public interface IV4DashboardSummaryService
{
    Task<DashboardSummaryReadDto> GetSummaryAsync(
        Guid userId,
        CancellationToken cancellationToken);
}

public interface IV4CategoriesService
{
    Task<IReadOnlyList<CategoryReadDto>> GetCategoriesAsync(
        Guid userId,
        CancellationToken cancellationToken);
}

public interface IV4TransactionService
{
    Task<TransactionListResponseDto> GetTransactionsAsync(
        Guid userId,
        V4TransactionQuery query,
        CancellationToken cancellationToken);

    Task<V4TransactionItemDto> QuickCreateAsync(
        Guid userId,
        QuickCreateTransactionRequest request,
        DateOnly businessDate,
        CancellationToken cancellationToken);
}

public interface IUserPreferenceService
{
    Task SaveAsync(
        Guid userId,
        Guid? defaultExpenseAccountId,
        Guid? defaultCategoryId,
        CancellationToken cancellationToken);
}

public interface ITransactionDefaultResolver
{
    Task<CreateTransactionDto> ResolveAsync(
        Guid userId,
        decimal amount,
        string? note,
        LifeDashboard.Domain.Enums.TransactionType transactionType,
        DateOnly businessDate,
        CancellationToken cancellationToken);
}
