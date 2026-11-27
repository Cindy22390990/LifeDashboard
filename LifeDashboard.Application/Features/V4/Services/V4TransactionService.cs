using LifeDashboard.Application.Features.V4.Dtos;
using LifeDashboard.Application.Features.V4.Interfaces;
using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Services.Interfaces;

namespace LifeDashboard.Application.Features.V4.Services;

public sealed class V4TransactionService : IV4TransactionService
{
    private readonly IV4TransactionReader _reader;
    private readonly ITransactionDefaultResolver _defaultResolver;
    private readonly ITransactionService _transactionService;

    public V4TransactionService(
        IV4TransactionReader reader,
        ITransactionDefaultResolver defaultResolver,
        ITransactionService transactionService)
    {
        _reader = reader;
        _defaultResolver = defaultResolver;
        _transactionService = transactionService;
    }

    public async Task<TransactionListResponseDto> GetTransactionsAsync(
        Guid userId,
        V4TransactionQuery query,
        CancellationToken cancellationToken)
    {
        var projection = await _reader.GetTransactionsAsync(
            userId,
            query,
            cancellationToken);

        var items = projection.Items
            .Select(item => new V4TransactionItemDto(
                item.Id,
                item.Note,
                item.Amount,
                item.TransactionType.ToString(),
                item.TransactionDate,
                item.AccountId,
                item.AccountName,
                item.CategoryId,
                item.CategoryName))
            .ToArray();

        return new TransactionListResponseDto(
            projection.TotalIncome,
            projection.TotalExpense,
            items);
    }

    public async Task<V4TransactionItemDto> QuickCreateAsync(
        Guid userId,
        QuickCreateTransactionRequest request,
        DateOnly businessDate,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(typeof(LifeDashboard.Domain.Enums.TransactionType), request.TransactionType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.TransactionType),
                "不支援的交易類型。");
        }

        var transactionType =
            (LifeDashboard.Domain.Enums.TransactionType)request.TransactionType;

        if (transactionType == LifeDashboard.Domain.Enums.TransactionType.Transfer)
        {
            throw new ArgumentException(
                "Quick Create 不支援轉帳交易。",
                nameof(request.TransactionType));
        }

        var createDto = await _defaultResolver.ResolveAsync(
            userId,
            request.Amount,
            request.Note,
            transactionType,
            businessDate,
            cancellationToken);

        var created = await _transactionService.CreateTransactionAsync(
            userId,
            createDto);

        return new V4TransactionItemDto(
            created.Id,
            created.Description,
            created.Amount,
            ((LifeDashboard.Domain.Enums.TransactionType)created.Type).ToString(),
            created.TransactionDate,
            created.AccountId,
            created.AccountName,
            created.CategoryEntityId,
            null);
    }
}
