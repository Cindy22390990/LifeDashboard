using LifeDashboard.Application.Models.DTOs;

namespace LifeDashboard.Application.Services.Interfaces
{
    public interface ITransactionService
    {
        Task<IEnumerable<TransactionDto>> GetAllTransactionsAsync();
        Task<TransactionDto> CreateTransactionAsync(CreateTransactionDto dto);
        Task DeleteTransactionAsync(Guid id);
    }
}