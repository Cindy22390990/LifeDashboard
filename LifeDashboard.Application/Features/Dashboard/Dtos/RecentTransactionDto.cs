using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Application.Features.Dashboard.Dtos;

public sealed record RecentTransactionDto(
    Guid Id,
    string? Note,
    string CategoryName,
    string AccountName,
    decimal Amount,
    TransactionType Type,
    string TransactionType,
    DateOnly TransactionDate);
