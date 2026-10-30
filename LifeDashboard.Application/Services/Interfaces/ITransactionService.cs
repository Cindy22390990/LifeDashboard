using LifeDashboard.Application.Models.DTOs;

namespace LifeDashboard.Application.Services.Interfaces
{
    public interface ITransactionService
    {
        Task<IEnumerable<TransactionDto>> GetAllTransactionsAsync(
            Guid userId);


        Task<TransactionDto> CreateTransactionAsync(
            Guid userId,
            CreateTransactionDto dto);


        Task DeleteTransactionAsync(
            Guid userId,
            Guid id);
    }
}