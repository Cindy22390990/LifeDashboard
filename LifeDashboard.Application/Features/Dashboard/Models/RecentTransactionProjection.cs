using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Application.Features.Dashboard.Models;

public sealed record RecentTransactionProjection(
    Guid Id,
    string Note,
    string CategoryName,
    string AccountName,
    decimal Amount,
    TransactionType Type,
    DateOnly TransactionDate);
