namespace LifeDashboard.Application.Features.V4.Dtos;

public sealed record V4TransactionItemDto(
    Guid Id,
    string? Note,
    decimal Amount,
    string TransactionType,
    DateOnly TransactionDate,
    Guid? AccountId,
    string? AccountName,
    Guid? CategoryId,
    string? CategoryName);

public sealed record TransactionListResponseDto(
    decimal TotalIncome,
    decimal TotalExpense,
    IReadOnlyList<V4TransactionItemDto> Items);

public sealed record QuickCreateTransactionRequest(
    decimal Amount,
    string? Note,
    int TransactionType);
