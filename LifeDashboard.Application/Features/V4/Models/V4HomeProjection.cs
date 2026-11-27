namespace LifeDashboard.Application.Features.V4.Models;

public sealed record V4HomeProjection(
    decimal AvailableMonthlyBudget,
    decimal ReservedFutureCommitments,
    decimal AccumulatedExpenseThroughYesterday,
    decimal DailySpent,
    int DaysInMonth,
    int ElapsedDays,
    int RemainingDays,
    V4MonthlyFinancialProgressProjection MonthlyProgress,
    IReadOnlyList<V4HomeTransactionProjection> TodayTransactions);

public sealed record V4MonthlyFinancialProgressProjection(
    decimal SavedAmount,
    decimal InvestedAmount,
    string Source);

public sealed record V4HomeTransactionProjection(
    Guid Id,
    string? Note,
    decimal Amount,
    LifeDashboard.Domain.Enums.TransactionType TransactionType,
    DateOnly TransactionDate);
