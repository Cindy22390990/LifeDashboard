using LifeDashboard.Application.Models;

namespace LifeDashboard.Application.Services.Interfaces;

public interface ITransferCore
{
    Task<TransferCoreResult> CreateTransferAsync(
        Guid userId,
        Guid sourceAccountId,
        Guid destinationAccountId,
        decimal amount,
        DateOnly transactionDate,
        string note);
}
