using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Application.Services.Interfaces;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Application.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IAccountRepository _accountRepository;

        public TransactionService(
            ITransactionRepository transactionRepository,
            IAccountRepository accountRepository)
        {
            _transactionRepository = transactionRepository;
            _accountRepository = accountRepository;
        }

        public async Task<IEnumerable<TransactionDto>> GetAllTransactionsAsync()
        {
            var transactions = await _transactionRepository.GetAllAsync();
            return transactions.Select(t => new TransactionDto
            {
                Id = t.Id,
                AccountId = t.AccountId,
                Amount = t.Amount,
                Type = (int)t.Type,
                CategoryId = t.Category,
                Description = t.Note,
                TransactionDate = t.TransactionDate
            });
        }

        public async Task<TransactionDto> CreateTransactionAsync(CreateTransactionDto dto)
        {
            var account = await _accountRepository.GetByIdAsync(dto.AccountId);
            if (account == null) throw new KeyNotFoundException("找不到指定的帳戶");

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = dto.AccountId,
                Amount = dto.Amount,
                Type = (TransactionType)dto.Type,
                Note = dto.Description ?? string.Empty,
                Category = string.Empty,
                TransactionDate = dto.TransactionDate,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            // 業務邏輯：連動更新帳戶餘額
            if (transaction.Type == TransactionType.Expense)
                account.CurrentBalance -= transaction.Amount;
            else if (transaction.Type == TransactionType.Income)
                account.CurrentBalance += transaction.Amount;

            await _accountRepository.UpdateAsync(account);
            var createdTx = await _transactionRepository.AddAsync(transaction);

            return new TransactionDto
            {
                Id = createdTx.Id,
                AccountId = createdTx.AccountId,
                Amount = createdTx.Amount,
                Type = (int)createdTx.Type,
                Description = createdTx.Note,
                TransactionDate = createdTx.TransactionDate
            };
        }

        public async Task DeleteTransactionAsync(Guid id)
        {
            var transaction = await _transactionRepository.GetByIdAsync(id);
            if (transaction == null) throw new KeyNotFoundException("找不到該筆交易紀錄");

            var account = await _accountRepository.GetByIdAsync(transaction.AccountId);
            if (account != null)
            {
                // 業務邏輯：刪除紀錄時，將帳戶餘額反向復原
                if (transaction.Type == TransactionType.Expense)
                    account.CurrentBalance += transaction.Amount;
                else if (transaction.Type == TransactionType.Income)
                    account.CurrentBalance -= transaction.Amount;

                await _accountRepository.UpdateAsync(account);
            }

            await _transactionRepository.DeleteAsync(transaction);
        }
    }
}