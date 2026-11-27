using LifeDashboard.Application.Features.V4.Services;
using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Domain.Enums;
using Moq;

namespace LifeDashboard.Tests.V4;

public sealed class UserPreferenceServiceTests
{
    [Fact]
    public async Task SaveAsync_persists_preference_after_cashflow_validation()
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
        var preferenceRepository = new Mock<IUserPreferenceRepository>();
        preferenceRepository
            .Setup(repository => repository.GetByUserIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPreference?)null);
        preferenceRepository
            .Setup(repository => repository.SaveAsync(
                It.IsAny<UserPreference>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var validator = new UserPreferenceValidator(
            accountRepository.Object,
            Mock.Of<ICategoryRepository>());
        var service = new UserPreferenceService(
            preferenceRepository.Object,
            validator);

        await service.SaveAsync(
            userId,
            account.Id,
            null,
            CancellationToken.None);

        preferenceRepository.Verify(
            repository => repository.SaveAsync(
                It.Is<UserPreference>(preference =>
                    preference.UserId == userId &&
                    preference.DefaultExpenseAccountId == account.Id),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task SaveAsync_does_not_persist_non_cashflow_preference()
    {
        var userId = Guid.NewGuid();
        var account = new Account(
            userId,
            "Savings",
            AccountType.Saving,
            100m);
        var accountRepository = new Mock<IAccountRepository>();
        accountRepository
            .Setup(repository => repository.GetByIdAsync(account.Id, userId))
            .ReturnsAsync(account);
        var preferenceRepository = new Mock<IUserPreferenceRepository>();
        var service = new UserPreferenceService(
            preferenceRepository.Object,
            new UserPreferenceValidator(
                accountRepository.Object,
                Mock.Of<ICategoryRepository>()));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SaveAsync(
                userId,
                account.Id,
                null,
                CancellationToken.None));

        preferenceRepository.Verify(
            repository => repository.SaveAsync(
                It.IsAny<UserPreference>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
