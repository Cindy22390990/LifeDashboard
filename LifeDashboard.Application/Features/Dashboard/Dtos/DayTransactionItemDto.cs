namespace LifeDashboard.Application.Features.Dashboard.Dtos;

public sealed record DayTransactionItemDto(
    Guid Id,
    string Note,
    string CategoryName,
    string AccountName,
    decimal Amount,
    string Type,
    DateOnly TransactionDate);
