using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Application.Features.V4.Services;

public sealed class UserPreferenceValidator
{
    private readonly IAccountRepository _accountRepository;
    private readonly ICategoryRepository _categoryRepository;

    public UserPreferenceValidator(
        IAccountRepository accountRepository,
        ICategoryRepository categoryRepository)
    {
        _accountRepository = accountRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task ValidateAsync(
        Guid userId,
        Guid? defaultExpenseAccountId,
        Guid? defaultCategoryId,
        CancellationToken cancellationToken)
    {
        if (defaultExpenseAccountId.HasValue)
        {
            var account = await _accountRepository.GetByIdAsync(
                defaultExpenseAccountId.Value,
                userId);

            if (account is null
                || account.IsDeleted
                || account.IsSystemAccount
                || account.Type != AccountType.Cashflow)
            {
                throw new ArgumentException(
                    "Default expense account must be an active Cashflow account owned by the user.",
                    nameof(defaultExpenseAccountId));
            }
        }

        if (defaultCategoryId.HasValue)
        {
            var category = await _categoryRepository.GetByIdAsync(
                defaultCategoryId.Value,
                userId);

            if (category is null)
            {
                throw new ArgumentException(
                    "Default category must be owned by the user.",
                    nameof(defaultCategoryId));
            }
        }
    }
}
