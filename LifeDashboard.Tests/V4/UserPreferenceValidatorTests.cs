using LifeDashboard.Application.Features.V4.Services;
using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Domain.Enums;
using Moq;

namespace LifeDashboard.Tests.V4;

public sealed class UserPreferenceValidatorTests
{
    [Fact]
    public async Task ValidateAsync_accepts_active_user_cashflow_account()
    {
        var userId = Guid.NewGuid();
        var account = new Account(
            userId,
            "Cashflow",
            AccountType.Cashflow,
            100m);
        var accountRepository = new Mock<IAccountRepository>();
        accountRepository
            .Setup(repository => repository.GetByIdAsync(account.Id, userId))
            .ReturnsAsync(account);
        var categoryRepository = new Mock<ICategoryRepository>();
        var validator = new UserPreferenceValidator(
            accountRepository.Object,
            categoryRepository.Object);

        await validator.ValidateAsync(
            userId,
            account.Id,
            null,
            CancellationToken.None);
    }

    [Theory]
    [InlineData(AccountType.Saving, false, false)]
    [InlineData(AccountType.Cashflow, true, false)]
    [InlineData(AccountType.Cashflow, false, true)]
    public async Task ValidateAsync_rejects_non_cashflow_inactive_or_system_account(
        AccountType accountType,
        bool isDeleted,
        bool isSystemAccount)
    {
        var userId = Guid.NewGuid();
        var account = new Account(
            userId,
            "Account",
            accountType,
            100m,
            isSystemAccount);
        if (isDeleted)
        {
            account.MarkAsDeleted();
        }

        var accountRepository = new Mock<IAccountRepository>();
        accountRepository
            .Setup(repository => repository.GetByIdAsync(account.Id, userId))
            .ReturnsAsync(account);
        var validator = new UserPreferenceValidator(
            accountRepository.Object,
            Mock.Of<ICategoryRepository>());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            validator.ValidateAsync(
                userId,
                account.Id,
                null,
                CancellationToken.None));
    }

    [Fact]
    public async Task ValidateAsync_rejects_account_not_owned_by_user()
    {
        var userId = Guid.NewGuid();
        var accountRepository = new Mock<IAccountRepository>();
        accountRepository
            .Setup(repository => repository.GetByIdAsync(
                It.IsAny<Guid>(),
                userId))
            .ReturnsAsync((Account?)null);
        var validator = new UserPreferenceValidator(
            accountRepository.Object,
            Mock.Of<ICategoryRepository>());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            validator.ValidateAsync(
                userId,
                Guid.NewGuid(),
                null,
                CancellationToken.None));
    }

    [Fact]
    public async Task ValidateAsync_rejects_category_not_owned_by_user()
    {
        var userId = Guid.NewGuid();
        var categoryRepository = new Mock<ICategoryRepository>();
        categoryRepository
            .Setup(repository => repository.GetByIdAsync(
                It.IsAny<Guid>(),
                userId))
            .ReturnsAsync((Category?)null);
        var validator = new UserPreferenceValidator(
            Mock.Of<IAccountRepository>(),
            categoryRepository.Object);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            validator.ValidateAsync(
                userId,
                null,
                Guid.NewGuid(),
                CancellationToken.None));
    }
}
