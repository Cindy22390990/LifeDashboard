using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Domain.Entities;

namespace LifeDashboard.Application.Services.Interfaces
{
    public interface ITransactionService
    {
        Task<IEnumerable<TransactionDto>> GetAllTransactionsAsync(
            Guid userId);


        Task<TransactionDto> CreateTransactionAsync(
            Guid userId,
            CreateTransactionDto dto);


        Task<TransactionDto> CreateTransactionWithinExistingTransactionAsync(
            Guid userId,
            CreateTransactionDto dto,
            Goal goal);


        Task DeleteTransactionAsync(
            Guid userId,
            Guid id);
    }
}