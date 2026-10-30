using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Domain.Entities;

public class Transaction
{
    public Guid Id { get; private set; }

    // 所屬使用者
    public Guid UserId { get; private set; }


    // 使用者導覽
    public User User { get; set; } = null!;

    public Transaction(
            Guid userId,
            Guid accountId,
            decimal amount,
            TransactionType type,
            string note,
            DateOnly transactionDate)
        {
            Id = Guid.NewGuid();

            UserId = userId;

            AccountId = accountId;

            Amount = amount;

            Type = type;

            Note = note;

            TransactionDate = transactionDate;

            CreatedAtUtc = DateTime.UtcNow;

            UpdatedAtUtc = DateTime.UtcNow;
        }
    private Transaction()
    {
    }


    // 交易類型：支出、收入、轉帳
    public TransactionType Type { get; private set; }


    // 金額
    public decimal Amount { get; private set; }


    // 分類，例如：飲食、交通、薪資
    public string Category { get; private set; } = string.Empty;

    // 備註
    public string Note { get; private set; } = string.Empty;

    // 交易日期
    public DateOnly TransactionDate { get; private set; }


    // 使用哪個帳戶
    public Guid AccountId { get; private set; }


    // 對應帳戶
    public Account Account { get; private set; } = null!;


    // 轉帳目的帳戶
    // 如果不是轉帳，可以為 null
    public Guid? ToAccountId { get; private set; }

    public Account? ToAccount { get; private set; }


    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }
}