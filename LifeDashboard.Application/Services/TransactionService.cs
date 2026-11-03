using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Exceptions;
using LifeDashboard.Application.Interfaces;
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
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public TransactionService(
            ITransactionRepository transactionRepository,
            IAccountRepository accountRepository,
            ICategoryRepository categoryRepository,
            IUnitOfWork unitOfWork)
        {
            _transactionRepository = transactionRepository;
            _accountRepository = accountRepository;
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<TransactionDto>> GetAllTransactionsAsync(
            Guid userId)
        {
            var transactions =
                await _transactionRepository
                .GetAllAsync(userId);


            return transactions.Select(t => new TransactionDto
            {
                Id = t.Id,
                AccountId = t.AccountId,
                AccountName = t.Account?.Name,
                ToAccountId = t.ToAccountId,
                ToAccountName = t.ToAccount?.Name,
                Amount = t.Amount,
                Type = (int)t.Type,
                CategoryId = t.Category,
                CategoryEntityId = t.CategoryId,
                Description = t.Note,
                TransactionDate = t.TransactionDate
            });
        }

        public async Task<TransactionDto> CreateTransactionAsync(
            Guid userId,
            CreateTransactionDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (dto.Amount <= 0)
                {
                    throw new ArgumentException("交易金額必須大於 0。", nameof(dto.Amount));
                }

                var type = (TransactionType)dto.Type;
                var account = await _accountRepository.GetByIdAsync(dto.AccountId, userId);
                if (account == null)
                {
                    if (type == TransactionType.Transfer)
                    {
                        throw new ArgumentException("找不到指定的來源帳戶。", nameof(dto.AccountId));
                    }

                    throw new KeyNotFoundException("找不到指定的帳戶");
                }

                Account? toAccount = null;
                if (type == TransactionType.Transfer)
                {
                    if (!dto.ToAccountId.HasValue)
                    {
                        throw new ArgumentException("轉帳必須指定轉入帳戶。", nameof(dto.ToAccountId));
                    }

                    if (dto.AccountId == dto.ToAccountId.Value)
                    {
                        throw new ArgumentException("來源帳戶與轉入帳戶不可相同。", nameof(dto.ToAccountId));
                    }

                    toAccount = await _accountRepository.GetByIdAsync(dto.ToAccountId.Value, userId);
                    if (toAccount == null)
                    {
                        throw new ArgumentException("找不到指定的轉入帳戶。", nameof(dto.ToAccountId));
                    }

                    if (account.CurrentBalance < dto.Amount)
                    {
                        throw new InsufficientFundsException("來源帳戶餘額不足，無法完成轉帳。");
                    }
                }

                Category? category = null;
                if (dto.CategoryId.HasValue)
                {
                    category = await _categoryRepository.GetByIdAsync(
                        dto.CategoryId.Value,
                        userId);

                    if (category == null)
                    {
                        throw new KeyNotFoundException("找不到指定的分類。");
                    }
                }

                var transaction = new Transaction(
                    userId,
                    dto.AccountId,
                    dto.Amount,
                    type,
                    dto.Description ?? string.Empty,
                    dto.TransactionDate,
                    toAccount?.Id
                );
                transaction.CategoryId = category?.Id;
                transaction.CategoryEntity = category;

                if (type == TransactionType.Transfer)
                {
                    account.CurrentBalance -= transaction.Amount;
                    toAccount!.CurrentBalance += transaction.Amount;
                }
                else if (type == TransactionType.Expense)
                {
                    account.CurrentBalance -= transaction.Amount;
                }
                else if (type == TransactionType.Income)
                {
                    account.CurrentBalance += transaction.Amount;
                }

                await _transactionRepository.AddAsync(transaction);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();

                return new TransactionDto
                {
                    Id = transaction.Id,

                    AccountId = transaction.AccountId,

                    AccountName = account.Name,

                    ToAccountId = transaction.ToAccountId,

                    ToAccountName = toAccount?.Name,

                    Amount = transaction.Amount,

                    Type = (int)transaction.Type,

                    CategoryId = transaction.Category,

                    CategoryEntityId = transaction.CategoryId,

                    Description = transaction.Note,

                    TransactionDate = transaction.TransactionDate
                };
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteTransactionAsync(
            Guid userId,
            Guid id)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var transaction =
                    await _transactionRepository.GetByIdAsync(
                        id,
                        userId);

                if (transaction == null)
                {
                    throw new KeyNotFoundException("找不到交易紀錄");
                }

                var account =
                    await _accountRepository.GetByIdAsync(
                        transaction.AccountId,
                        userId);

                if (transaction.Type == TransactionType.Transfer)
                {
                    if (account == null || !transaction.ToAccountId.HasValue)
                    {
                        throw new InvalidOperationException("轉帳交易缺少有效的來源或轉入帳戶。");
                    }

                    var toAccount = await _accountRepository.GetByIdAsync(
                        transaction.ToAccountId.Value,
                        userId);
                    if (toAccount == null)
                    {
                        throw new InvalidOperationException("找不到轉帳的轉入帳戶。");
                    }

                    account.CurrentBalance += transaction.Amount;
                    toAccount.CurrentBalance -= transaction.Amount;
                }
                else if (account != null)
                {
                    if (transaction.Type == TransactionType.Expense)
                    {
                        account.CurrentBalance += transaction.Amount;
                    }
                    else if (transaction.Type == TransactionType.Income)
                    {
                        account.CurrentBalance -= transaction.Amount;
                    }
                }

                _transactionRepository.Delete(transaction);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }
    }
}