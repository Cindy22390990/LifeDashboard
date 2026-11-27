using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Application.Exceptions;
using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Application.Features.V4.Services;

public sealed class TransactionDefaultResolver : ITransactionDefaultResolver
{
    private readonly IAccountRepository _accountRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUserPreferenceRepository _preferenceRepository;

    public TransactionDefaultResolver(
        IAccountRepository accountRepository,
        ICategoryRepository categoryRepository,
        ITransactionRepository transactionRepository,
        IUserPreferenceRepository preferenceRepository)
    {
        _accountRepository = accountRepository;
        _categoryRepository = categoryRepository;
        _transactionRepository = transactionRepository;
        _preferenceRepository = preferenceRepository;
    }

    public async Task<CreateTransactionDto> ResolveAsync(
        Guid userId,
        decimal amount,
        string? note,
        TransactionType transactionType,
        DateOnly businessDate,
        CancellationToken cancellationToken)
    {
        if (amount <= 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "交易金額必須大於 0。");
        }

        var accounts = (await _accountRepository.GetAllAsync(userId))
            .Where(account =>
                account.Type == AccountType.Cashflow
                && !account.IsDeleted
                && !account.IsSystemAccount)
            .ToArray();
        var preference = await _preferenceRepository.GetByUserIdAsync(
            userId,
            cancellationToken);

        var defaultAccount = preference?.DefaultExpenseAccountId.HasValue == true
            ? accounts.FirstOrDefault(account =>
                account.Id == preference.DefaultExpenseAccountId.Value)
            : null;

        if (defaultAccount == null)
        {
            var recentAccountId =
                await _transactionRepository.GetMostUsedExpenseAccountIdAsync(
                    userId,
                    businessDate.AddDays(-29),
                    cancellationToken);

            defaultAccount = recentAccountId.HasValue
                ? accounts.FirstOrDefault(account =>
                    account.Id == recentAccountId.Value)
                : null;
        }

        defaultAccount ??= accounts
            .OrderBy(account => account.CreatedAtUtc)
            .ThenBy(account => account.Id)
            .FirstOrDefault();

        if (defaultAccount == null)
        {
            throw new NoCashflowAccountException();
        }

        Guid? categoryId = null;
        if (preference?.DefaultCategoryId.HasValue == true)
        {
            var configuredCategory = await _categoryRepository.GetByIdAsync(
                preference.DefaultCategoryId.Value,
                userId);
            if (configuredCategory != null)
            {
                categoryId = configuredCategory.Id;
            }
        }

        if (!categoryId.HasValue)
        {
            categoryId = await _transactionRepository.GetRecentCategoryIdAsync(
                userId,
                cancellationToken);
        }

        if (!categoryId.HasValue)
        {
            categoryId = await _transactionRepository.GetMostUsedCategoryIdAsync(
                userId,
                businessDate.AddDays(-29),
                cancellationToken);
        }

        return new CreateTransactionDto
        {
            AccountId = defaultAccount.Id,
            Amount = amount,
            Type = (int)transactionType,
            CategoryId = categoryId,
            Description = note?.Trim() ?? string.Empty,
            TransactionDate = businessDate
        };
    }
}
