using LifeDashboard.Application.Exceptions;
using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Application.Services;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Domain.Enums;
using Moq;

namespace LifeDashboard.Tests.Transaction;

public class TransferCoreTests
{
    [Fact]
    public async Task CreateTransferAsync_updates_balances_and_adds_transfer_transaction()
    {
        var userId = Guid.NewGuid();
        var sourceAccount = CreateAccount(userId, "Source", 100m);
        var destinationAccount = CreateAccount(userId, "Destination", 20m);
        var accountRepository = new Mock<IAccountRepository>();
        accountRepository
            .Setup(repository => repository.GetByIdAsync(sourceAccount.Id, userId))
            .ReturnsAsync(sourceAccount);
        accountRepository
            .Setup(repository => repository.GetByIdAsync(destinationAccount.Id, userId))
            .ReturnsAsync(destinationAccount);

        var transactionRepository = new Mock<ITransactionRepository>();
        transactionRepository
            .Setup(repository => repository.AddAsync(
                It.IsAny<LifeDashboard.Domain.Entities.Transaction>()))
            .Returns(Task.CompletedTask);
        var transferCore = new TransferCore(
            accountRepository.Object,
            transactionRepository.Object,
            new AccountDebitService());

        var result = await transferCore.CreateTransferAsync(
            userId,
            sourceAccount.Id,
            destinationAccount.Id,
            35m,
            new DateOnly(2026, 11, 3),
            "Goal deposit transfer");

        Assert.Equal(65m, sourceAccount.CurrentBalance);
        Assert.Equal(55m, destinationAccount.CurrentBalance);
        Assert.Equal(TransactionType.Transfer, result.Transaction.Type);
        Assert.Equal(sourceAccount.Id, result.Transaction.AccountId);
        Assert.Equal(destinationAccount.Id, result.Transaction.ToAccountId);
        Assert.Equal(35m, result.Transaction.Amount);
        transactionRepository.Verify(
            repository => repository.AddAsync(result.Transaction),
            Times.Once);
    }

    [Fact]
    public async Task CreateTransferAsync_rejects_insufficient_funds_without_mutating_accounts()
    {
        var userId = Guid.NewGuid();
        var sourceAccount = CreateAccount(userId, "Source", 10m);
        var destinationAccount = CreateAccount(userId, "Destination", 20m);
        var accountRepository = new Mock<IAccountRepository>();
        accountRepository
            .Setup(repository => repository.GetByIdAsync(sourceAccount.Id, userId))
            .ReturnsAsync(sourceAccount);
        accountRepository
            .Setup(repository => repository.GetByIdAsync(destinationAccount.Id, userId))
            .ReturnsAsync(destinationAccount);

        var transactionRepository = new Mock<ITransactionRepository>();
        var transferCore = new TransferCore(
            accountRepository.Object,
            transactionRepository.Object,
            new AccountDebitService());

        await Assert.ThrowsAsync<InsufficientFundsException>(() =>
            transferCore.CreateTransferAsync(
                userId,
                sourceAccount.Id,
                destinationAccount.Id,
                35m,
                new DateOnly(2026, 11, 3),
                string.Empty));

        Assert.Equal(10m, sourceAccount.CurrentBalance);
        Assert.Equal(20m, destinationAccount.CurrentBalance);
        transactionRepository.Verify(
            repository => repository.AddAsync(
                It.IsAny<LifeDashboard.Domain.Entities.Transaction>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateTransferAsync_allows_source_to_go_negative_when_configured()
    {
        var userId = Guid.NewGuid();
        var sourceAccount = CreateAccount(userId, "Source", 10m);
        sourceAccount.AllowNegativeBalance = true;
        var destinationAccount = CreateAccount(userId, "Destination", 20m);
        destinationAccount.AllowNegativeBalance = false;
        var accountRepository = new Mock<IAccountRepository>();
        accountRepository
            .Setup(repository => repository.GetByIdAsync(sourceAccount.Id, userId))
            .ReturnsAsync(sourceAccount);
        accountRepository
            .Setup(repository => repository.GetByIdAsync(destinationAccount.Id, userId))
            .ReturnsAsync(destinationAccount);

        var transactionRepository = new Mock<ITransactionRepository>();
        transactionRepository
            .Setup(repository => repository.AddAsync(
                It.IsAny<LifeDashboard.Domain.Entities.Transaction>()))
            .Returns(Task.CompletedTask);
        var transferCore = new TransferCore(
            accountRepository.Object,
            transactionRepository.Object,
            new AccountDebitService());

        await transferCore.CreateTransferAsync(
            userId,
            sourceAccount.Id,
            destinationAccount.Id,
            35m,
            new DateOnly(2026, 11, 3),
            "Configured overdraft");

        Assert.Equal(-25m, sourceAccount.CurrentBalance);
        Assert.Equal(55m, destinationAccount.CurrentBalance);
    }

    private static Account CreateAccount(Guid userId, string name, decimal balance)
    {
        var account = new Account(userId, name, AccountType.Cashflow, balance)
        {
            CurrentBalance = balance
        };
        return account;
    }
}
