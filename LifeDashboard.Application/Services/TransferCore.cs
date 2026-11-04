using LifeDashboard.Application.Models;
using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Application.Services.Interfaces;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Application.Services;

public class TransferCore : ITransferCore
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountDebitService _accountDebitService;

    public TransferCore(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IAccountDebitService accountDebitService)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _accountDebitService = accountDebitService;
    }

    public async Task<TransferCoreResult> CreateTransferAsync(
        Guid userId,
        Guid sourceAccountId,
        Guid destinationAccountId,
        decimal amount,
        DateOnly transactionDate,
        string note)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("交易金額必須大於 0。", nameof(amount));
        }

        if (sourceAccountId == destinationAccountId)
        {
            throw new ArgumentException("來源帳戶與轉入帳戶不可相同。", nameof(destinationAccountId));
        }

        var sourceAccount = await _accountRepository.GetByIdAsync(sourceAccountId, userId);
        if (sourceAccount is null)
        {
            throw new ArgumentException("找不到指定的來源帳戶。", nameof(sourceAccountId));
        }

        var destinationAccount = await _accountRepository.GetByIdAsync(destinationAccountId, userId);
        if (destinationAccount is null)
        {
            throw new ArgumentException("找不到指定的轉入帳戶。", nameof(destinationAccountId));
        }

        _accountDebitService.Debit(sourceAccount, amount);

        var transaction = new Transaction(
            userId,
            sourceAccountId,
            amount,
            TransactionType.Transfer,
            note,
            transactionDate,
            destinationAccountId);

        destinationAccount.CurrentBalance += amount;
        await _transactionRepository.AddAsync(transaction);

        return new TransferCoreResult(transaction, sourceAccount, destinationAccount);
    }
}
