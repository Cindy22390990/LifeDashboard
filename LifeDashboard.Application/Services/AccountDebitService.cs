using LifeDashboard.Application.Exceptions;
using LifeDashboard.Application.Services.Interfaces;
using LifeDashboard.Domain.Entities;

namespace LifeDashboard.Application.Services;

public sealed class AccountDebitService : IAccountDebitService
{
    public void Debit(Account account, decimal amount)
    {
        ArgumentNullException.ThrowIfNull(account);

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "扣款金額必須大於 0。");
        }

        if (!account.AllowNegativeBalance && account.CurrentBalance < amount)
        {
            throw new InsufficientFundsException("帳戶餘額不足，無法完成扣款。");
        }

        account.CurrentBalance -= amount;
    }
}
