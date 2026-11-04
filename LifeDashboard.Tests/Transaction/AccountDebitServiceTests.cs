using LifeDashboard.Application.Exceptions;
using LifeDashboard.Application.Services;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Tests.Transaction;

public class AccountDebitServiceTests
{
    private readonly AccountDebitService _service = new();

    [Fact]
    public void Debit_allows_sufficient_funds_when_negative_balance_is_disabled()
    {
        var account = CreateAccount(100m, allowNegativeBalance: false);

        _service.Debit(account, 40m);

        Assert.Equal(60m, account.CurrentBalance);
    }

    [Fact]
    public void Debit_rejects_insufficient_funds_without_mutating_the_account()
    {
        var account = CreateAccount(10m, allowNegativeBalance: false);

        Assert.Throws<InsufficientFundsException>(() => _service.Debit(account, 40m));

        Assert.Equal(10m, account.CurrentBalance);
    }

    [Fact]
    public void Debit_allows_negative_balance_when_enabled()
    {
        var account = CreateAccount(10m, allowNegativeBalance: true);

        _service.Debit(account, 40m);

        Assert.Equal(-30m, account.CurrentBalance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Debit_rejects_non_positive_amount_without_mutating_the_account(decimal amount)
    {
        var account = CreateAccount(10m, allowNegativeBalance: true);

        Assert.Throws<ArgumentOutOfRangeException>(() => _service.Debit(account, amount));

        Assert.Equal(10m, account.CurrentBalance);
    }

    private static Account CreateAccount(decimal balance, bool allowNegativeBalance)
    {
        return new Account(
            Guid.NewGuid(),
            "Test account",
            AccountType.Cashflow,
            balance,
            allowNegativeBalance: allowNegativeBalance)
        {
            CurrentBalance = balance
        };
    }
}
