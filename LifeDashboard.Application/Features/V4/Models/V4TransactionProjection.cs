namespace LifeDashboard.Application.Features.V4.Models;

public sealed record V4TransactionProjection(
    Guid Id,
    string? Note,
    decimal Amount,
    LifeDashboard.Domain.Enums.TransactionType TransactionType,
    DateOnly TransactionDate,
    Guid? AccountId,
    string? AccountName,
    Guid? CategoryId,
    string? CategoryName);

public sealed record V4TransactionListProjection(
    decimal TotalIncome,
    decimal TotalExpense,
    IReadOnlyList<V4TransactionProjection> Items);
