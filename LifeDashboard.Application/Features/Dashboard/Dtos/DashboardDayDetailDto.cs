namespace LifeDashboard.Application.Features.Dashboard.Dtos;

public sealed record DashboardDayDetailDto(
    DateOnly Date,
    decimal TotalIncome,
    decimal TotalExpense,
    IReadOnlyList<DayTransactionItemDto> Transactions);
