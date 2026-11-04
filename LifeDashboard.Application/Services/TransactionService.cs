using LifeDashboard.Application.Models.DTOs;
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
        private readonly IAccountDebitService _accountDebitService;
        private readonly ITransferCore _transferCore;
        private readonly IUnitOfWork _unitOfWork;

        public TransactionService(
            ITransactionRepository transactionRepository,
            IAccountRepository accountRepository,
            ICategoryRepository categoryRepository,
            IAccountDebitService accountDebitService,
            ITransferCore transferCore,
            IUnitOfWork unitOfWork)
        {
            _transactionRepository = transactionRepository;
            _accountRepository = accountRepository;
            _categoryRepository = categoryRepository;
            _accountDebitService = accountDebitService;
            _transferCore = transferCore;
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

                Transaction transaction;
                Account account;
                Account? toAccount = null;

                if (type == TransactionType.Transfer)
                {
                    if (!dto.ToAccountId.HasValue)
                    {
                        throw new ArgumentException("轉帳必須指定轉入帳戶。", nameof(dto.ToAccountId));
                    }

                    var transfer = await _transferCore.CreateTransferAsync(
                        userId,
                        dto.AccountId,
                        dto.ToAccountId.Value,
                        dto.Amount,
                        dto.TransactionDate,
                        dto.Description ?? string.Empty);
                    transaction = transfer.Transaction;
                    account = transfer.SourceAccount;
                    toAccount = transfer.DestinationAccount;
                }
                else
                {
                    account = await _accountRepository.GetByIdAsync(dto.AccountId, userId)
                        ?? throw new KeyNotFoundException("找不到指定的帳戶");

                    transaction = new Transaction(
                        userId,
                        dto.AccountId,
                        dto.Amount,
                        type,
                        dto.Description ?? string.Empty,
                        dto.TransactionDate);
                }

                transaction.CategoryId = category?.Id;
                transaction.CategoryEntity = category;

                if (type == TransactionType.Expense)
                {
                    _accountDebitService.Debit(account, transaction.Amount);
                }
                else if (type == TransactionType.Income)
                {
                    account.CurrentBalance += transaction.Amount;
                }

                if (type != TransactionType.Transfer)
                {
                    await _transactionRepository.AddAsync(transaction);
                }
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

                if (transaction.GoalId.HasValue)
                {
                    throw new InvalidOperationException("關聯目標的交易不可直接刪除。");
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

                    _accountDebitService.Debit(toAccount, transaction.Amount);
                    account.CurrentBalance += transaction.Amount;
                }
                else if (account != null)
                {
                    if (transaction.Type == TransactionType.Expense)
                    {
                        account.CurrentBalance += transaction.Amount;
                    }
                    else if (transaction.Type == TransactionType.Income)
                    {
                        _accountDebitService.Debit(account, transaction.Amount);
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