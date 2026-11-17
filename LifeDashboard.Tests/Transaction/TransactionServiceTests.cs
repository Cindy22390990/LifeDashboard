using LifeDashboard.Application.Interfaces;
using LifeDashboard.Application.Exceptions;
using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Application.Services;
using LifeDashboard.Application.Services.Interfaces;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Domain.Enums;
using Moq;
using TransactionEntity = LifeDashboard.Domain.Entities.Transaction;

namespace LifeDashboard.Tests.Transaction;

public class TransactionServiceTests
{
    [Theory]
    [InlineData(TransactionType.Expense)]
    [InlineData(TransactionType.Income)]
    public async Task Delete_with_missing_account_rolls_back_without_mutating_transaction(
        TransactionType type)
    {
        var userId = Guid.NewGuid();
        var transaction = new TransactionEntity(
            userId,
            Guid.NewGuid(),
            25m,
            type,
            "Missing account test",
            new DateOnly(2026, 11, 16));
        var transactionRepository = new Mock<ITransactionRepository>();
        transactionRepository
            .Setup(repository => repository.GetByIdAsync(transaction.Id, userId))
            .ReturnsAsync(transaction);
        var accountRepository = new Mock<IAccountRepository>();
        accountRepository
            .Setup(repository => repository.GetByIdAsync(transaction.AccountId, userId))
            .ReturnsAsync((Account?)null);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(work => work.BeginTransactionAsync())
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(work => work.RollbackAsync())
            .Returns(Task.CompletedTask);

        var service = new TransactionService(
            transactionRepository.Object,
            accountRepository.Object,
            new Mock<ICategoryRepository>().Object,
            new AccountDebitService(),
            new Mock<ITransferCore>().Object,
            unitOfWork.Object);

        await Assert.ThrowsAsync<AccountNotFoundException>(
            () => service.DeleteTransactionAsync(userId, transaction.Id));

        Assert.False(transaction.IsDeleted);
        Assert.Null(transaction.DeletedAt);
        Assert.Null(transaction.DeletedReason);
        transactionRepository.Verify(
            repository => repository.Delete(It.IsAny<TransactionEntity>()),
            Times.Never);
        unitOfWork.Verify(work => work.SaveChangesAsync(), Times.Never);
        unitOfWork.Verify(work => work.CommitAsync(), Times.Never);
        unitOfWork.Verify(work => work.RollbackAsync(), Times.Once);
    }
}
