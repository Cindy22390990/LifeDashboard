using LifeDashboard.Application.Features.V4.Services;
using LifeDashboard.Application.Exceptions;
using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Domain.Enums;
using Moq;

namespace LifeDashboard.Tests.V4;

public sealed class TransactionDefaultResolverTests
{
    [Fact]
    public async Task Default_account_exists_is_preferred()
    {
        var preferred = new Account(
            Guid.NewGuid(),
            "Preferred",
            AccountType.Cashflow,
            1_000m);
        var fallback = new Account(
            Guid.NewGuid(),
            "Fallback",
            AccountType.Cashflow,
            500m);
        var preference = new UserPreference(
            preferred.UserId,
            preferred.Id);

        var sut = CreateResolver(preferred, fallback, preference);
        var result = await sut.ResolveAsync(
            preferred.UserId,
            100m,
            "Lunch",
            TransactionType.Expense,
            new DateOnly(2026, 11, 25),
            CancellationToken.None);

        Assert.Equal(preferred.Id, result.AccountId);
    }

    [Fact]
    public async Task Non_cashflow_preference_falls_back_to_cashflow_account()
    {
        var userId = Guid.NewGuid();
        var nonCashflow = new Account(
            userId,
            "Savings",
            AccountType.Saving,
            1_000m);
        var cashflow = new Account(
            userId,
            "Cashflow",
            AccountType.Cashflow,
            500m);
        var preference = new UserPreference(userId, nonCashflow.Id);
        var sut = CreateResolver(nonCashflow, cashflow, preference);

        var result = await sut.ResolveAsync(
            userId,
            100m,
            null,
            TransactionType.Expense,
            new DateOnly(2026, 11, 25),
            CancellationToken.None);

        Assert.Equal(cashflow.Id, result.AccountId);
    }

    [Fact]
    public async Task Falls_back_to_first_active_cashflow_account()
    {
        var userId = Guid.NewGuid();
        var cashflow = new Account(
            userId,
            "Cashflow",
            AccountType.Cashflow,
            1_000m);
        var sut = CreateResolver(cashflow, cashflow, null);

        var result = await sut.ResolveAsync(
            userId,
            100m,
            null,
            TransactionType.Expense,
            new DateOnly(2026, 11, 25),
            CancellationToken.None);

        Assert.Equal(cashflow.Id, result.AccountId);
    }

    [Fact]
    public async Task Invalid_preference_account_falls_back_to_recent_expense_account()
    {
        var userId = Guid.NewGuid();
        var accountA = new Account(
            userId,
            "A",
            AccountType.Cashflow,
            1_000m);
        var accountB = new Account(
            userId,
            "B",
            AccountType.Cashflow,
            500m);
        var missingPreferenceAccountId = Guid.NewGuid();
        var preference = new UserPreference(
            userId,
            missingPreferenceAccountId);

        var accountRepository = new Mock<IAccountRepository>();
        accountRepository
            .Setup(repository => repository.GetAllAsync(userId))
            .ReturnsAsync(new[] { accountA, accountB });

        var categoryRepository = new Mock<ICategoryRepository>();
        var transactionRepository = new Mock<ITransactionRepository>();
        transactionRepository
            .Setup(repository => repository.GetMostUsedExpenseAccountIdAsync(
                userId,
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountB.Id);
        transactionRepository
            .Setup(repository => repository.GetRecentCategoryIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);
        transactionRepository
            .Setup(repository => repository.GetMostUsedCategoryIdAsync(
                userId,
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var preferenceRepository = new Mock<IUserPreferenceRepository>();
        preferenceRepository
            .Setup(repository => repository.GetByUserIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(preference);

        var sut = new TransactionDefaultResolver(
            accountRepository.Object,
            categoryRepository.Object,
            transactionRepository.Object,
            preferenceRepository.Object);

        var result = await sut.ResolveAsync(
            userId,
            100m,
            null,
            TransactionType.Expense,
            new DateOnly(2026, 11, 25),
            CancellationToken.None);

        Assert.Equal(accountB.Id, result.AccountId);
    }

    [Fact]
    public async Task Recent_expense_account_uses_most_used_account()
    {
        var userId = Guid.NewGuid();
        var accountA = new Account(
            userId,
            "A",
            AccountType.Cashflow,
            1_000m);
        var accountB = new Account(
            userId,
            "B",
            AccountType.Cashflow,
            500m);

        var accountRepository = new Mock<IAccountRepository>();
        accountRepository
            .Setup(repository => repository.GetAllAsync(userId))
            .ReturnsAsync(new[] { accountB, accountA });

        var categoryRepository = new Mock<ICategoryRepository>();
        var transactionRepository = new Mock<ITransactionRepository>();
        transactionRepository
            .Setup(repository => repository.GetMostUsedExpenseAccountIdAsync(
                userId,
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountA.Id);
        transactionRepository
            .Setup(repository => repository.GetRecentCategoryIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);
        transactionRepository
            .Setup(repository => repository.GetMostUsedCategoryIdAsync(
                userId,
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var preferenceRepository = new Mock<IUserPreferenceRepository>();
        preferenceRepository
            .Setup(repository => repository.GetByUserIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPreference?)null);

        var sut = new TransactionDefaultResolver(
            accountRepository.Object,
            categoryRepository.Object,
            transactionRepository.Object,
            preferenceRepository.Object);

        var result = await sut.ResolveAsync(
            userId,
            100m,
            null,
            TransactionType.Expense,
            new DateOnly(2026, 11, 25),
            CancellationToken.None);

        Assert.Equal(accountA.Id, result.AccountId);
    }

    [Fact]
    public async Task No_cashflow_account_returns_domain_exception()
    {
        var userId = Guid.NewGuid();
        var accountRepository = new Mock<IAccountRepository>();
        accountRepository
            .Setup(repository => repository.GetAllAsync(userId))
            .ReturnsAsync(Array.Empty<Account>());
        var categoryRepository = new Mock<ICategoryRepository>();
        var transactionRepository = new Mock<ITransactionRepository>();
        transactionRepository
            .Setup(repository => repository.GetMostUsedExpenseAccountIdAsync(
                userId,
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var preferenceRepository = new Mock<IUserPreferenceRepository>();
        preferenceRepository
            .Setup(repository => repository.GetByUserIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPreference?)null);

        var sut = new TransactionDefaultResolver(
            accountRepository.Object,
            categoryRepository.Object,
            transactionRepository.Object,
            preferenceRepository.Object);

        await Assert.ThrowsAsync<NoCashflowAccountException>(() =>
            sut.ResolveAsync(
                userId,
                100m,
                null,
                TransactionType.Expense,
                new DateOnly(2026, 11, 25),
                CancellationToken.None));
    }

    [Fact]
    public async Task Uses_recent_category_before_other_fallbacks()
    {
        var userId = Guid.NewGuid();
        var account = new Account(
            userId,
            "Cashflow",
            AccountType.Cashflow,
            1_000m);
        var recentCategoryId = Guid.NewGuid();
        var sut = CreateResolver(
            account,
            account,
            new UserPreference(userId, account.Id),
            recentCategoryId: recentCategoryId);

        var result = await sut.ResolveAsync(
            userId,
            100m,
            null,
            TransactionType.Expense,
            new DateOnly(2026, 11, 25),
            CancellationToken.None);

        Assert.Equal(recentCategoryId, result.CategoryId);
    }

    [Fact]
    public async Task Preference_category_is_used_before_recent_category()
    {
        var userId = Guid.NewGuid();
        var account = new Account(
            userId,
            "Cashflow",
            AccountType.Cashflow,
            1_000m);
        var preferenceCategoryId = Guid.NewGuid();
        var preference = new UserPreference(
            userId,
            account.Id,
            preferenceCategoryId);
        var preferenceCategory = new Category(
            userId,
            "Configured",
            "◇",
            BucketType.Living);

        var accountRepository = new Mock<IAccountRepository>();
        accountRepository
            .Setup(repository => repository.GetAllAsync(userId))
            .ReturnsAsync(new[] { account });

        var categoryRepository = new Mock<ICategoryRepository>();
        categoryRepository
            .Setup(repository => repository.GetByIdAsync(
                preferenceCategoryId,
                userId))
            .ReturnsAsync(preferenceCategory);

        var transactionRepository = new Mock<ITransactionRepository>();
        transactionRepository
            .Setup(repository => repository.GetMostUsedExpenseAccountIdAsync(
                userId,
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);
        transactionRepository
            .Setup(repository => repository.GetRecentCategoryIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        transactionRepository
            .Setup(repository => repository.GetMostUsedCategoryIdAsync(
                userId,
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var preferenceRepository = new Mock<IUserPreferenceRepository>();
        preferenceRepository
            .Setup(repository => repository.GetByUserIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(preference);

        var sut = new TransactionDefaultResolver(
            accountRepository.Object,
            categoryRepository.Object,
            transactionRepository.Object,
            preferenceRepository.Object);

        var result = await sut.ResolveAsync(
            userId,
            100m,
            null,
            TransactionType.Expense,
            new DateOnly(2026, 11, 25),
            CancellationToken.None);

        Assert.Equal(preferenceCategory.Id, result.CategoryId);
    }

    [Fact]
    public async Task Invalid_preference_category_falls_back_to_recent_category()
    {
        var userId = Guid.NewGuid();
        var account = new Account(
            userId,
            "Cashflow",
            AccountType.Cashflow,
            1_000m);
        var missingCategoryId = Guid.NewGuid();
        var recentCategoryId = Guid.NewGuid();
        var preference = new UserPreference(
            userId,
            account.Id,
            missingCategoryId);

        var accountRepository = new Mock<IAccountRepository>();
        accountRepository
            .Setup(repository => repository.GetAllAsync(userId))
            .ReturnsAsync(new[] { account });

        var categoryRepository = new Mock<ICategoryRepository>();
        categoryRepository
            .Setup(repository => repository.GetByIdAsync(
                missingCategoryId,
                userId))
            .ReturnsAsync((Category?)null);

        var transactionRepository = new Mock<ITransactionRepository>();
        transactionRepository
            .Setup(repository => repository.GetMostUsedExpenseAccountIdAsync(
                userId,
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);
        transactionRepository
            .Setup(repository => repository.GetRecentCategoryIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(recentCategoryId);
        transactionRepository
            .Setup(repository => repository.GetMostUsedCategoryIdAsync(
                userId,
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var preferenceRepository = new Mock<IUserPreferenceRepository>();
        preferenceRepository
            .Setup(repository => repository.GetByUserIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(preference);

        var sut = new TransactionDefaultResolver(
            accountRepository.Object,
            categoryRepository.Object,
            transactionRepository.Object,
            preferenceRepository.Object);

        var result = await sut.ResolveAsync(
            userId,
            100m,
            null,
            TransactionType.Expense,
            new DateOnly(2026, 11, 25),
            CancellationToken.None);

        Assert.Equal(recentCategoryId, result.CategoryId);
    }

    [Fact]
    public async Task Falls_back_to_uncategorized_without_category()
    {
        var userId = Guid.NewGuid();
        var account = new Account(
            userId,
            "Cashflow",
            AccountType.Cashflow,
            1_000m);
        var sut = CreateResolver(account, account, null);

        var result = await sut.ResolveAsync(
            userId,
            100m,
            null,
            TransactionType.Expense,
            new DateOnly(2026, 11, 25),
            CancellationToken.None);

        Assert.Null(result.CategoryId);
    }

    [Fact]
    public async Task Uses_most_used_category_when_recent_category_is_missing()
    {
        var userId = Guid.NewGuid();
        var account = new Account(
            userId,
            "Cashflow",
            AccountType.Cashflow,
            1_000m);
        var usedCategoryId = Guid.NewGuid();

        var accountRepository = new Mock<IAccountRepository>();
        accountRepository
            .Setup(repository => repository.GetAllAsync(userId))
            .ReturnsAsync(new[] { account });

        var categoryRepository = new Mock<ICategoryRepository>();

        var transactionRepository = new Mock<ITransactionRepository>();
        transactionRepository
            .Setup(repository => repository.GetRecentCategoryIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);
        transactionRepository
            .Setup(repository => repository.GetMostUsedCategoryIdAsync(
                userId,
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(usedCategoryId);

        var preferenceRepository = new Mock<IUserPreferenceRepository>();
        preferenceRepository
            .Setup(repository => repository.GetByUserIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((LifeDashboard.Domain.Entities.UserPreference?)null);

        var sut = new TransactionDefaultResolver(
            accountRepository.Object,
            categoryRepository.Object,
            transactionRepository.Object,
            preferenceRepository.Object);

        var result = await sut.ResolveAsync(
            userId,
            100m,
            null,
            TransactionType.Expense,
            new DateOnly(2026, 11, 25),
            CancellationToken.None);

        Assert.Equal(usedCategoryId, result.CategoryId);
    }

    private static TransactionDefaultResolver CreateResolver(
        Account preferredAccount,
        Account fallbackAccount,
        UserPreference? preference,
        Guid? recentCategoryId = null)
    {
        var accountRepository = new Mock<IAccountRepository>();
        accountRepository
            .Setup(repository => repository.GetAllAsync(
                It.IsAny<Guid>()))
            .ReturnsAsync(new[] { preferredAccount, fallbackAccount }
                .DistinctBy(account => account.Id));

        var categoryRepository = new Mock<ICategoryRepository>();
        categoryRepository
            .Setup(repository => repository.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>()))
            .ReturnsAsync((Guid id, Guid userId) =>
                new Category(userId, "Default", "◇", BucketType.Living));

        var transactionRepository = new Mock<ITransactionRepository>();
        transactionRepository
            .Setup(repository => repository.GetRecentCategoryIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(recentCategoryId);
        transactionRepository
            .Setup(repository => repository.GetMostUsedExpenseAccountIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);
        transactionRepository
            .Setup(repository => repository.GetMostUsedCategoryIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, DateOnly _, CancellationToken _) => null);

        var preferenceRepository = new Mock<IUserPreferenceRepository>();
        preferenceRepository
            .Setup(repository => repository.GetByUserIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(preference);

        return new TransactionDefaultResolver(
            accountRepository.Object,
            categoryRepository.Object,
            transactionRepository.Object,
            preferenceRepository.Object);
    }
}
