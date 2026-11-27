namespace LifeDashboard.Application.Features.V4.Dtos;

public sealed record DashboardV4HomeDto(
    DateOnly BusinessDate,
    decimal DailySpendingLimit,
    decimal DailySpent,
    decimal DailyRemaining,
    MonthlyFinancialProgressDto MonthlyProgress,
    IReadOnlyList<V4HomeTransactionItemDto> TodayTransactions);

public sealed record MonthlyFinancialProgressDto(
    decimal Saved,
    decimal Invested,
    string Source);

public sealed record V4HomeTransactionItemDto(
    Guid Id,
    string? Note,
    decimal Amount,
    string TransactionType,
    DateOnly TransactionDate);
