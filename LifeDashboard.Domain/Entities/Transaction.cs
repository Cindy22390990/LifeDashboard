using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Domain.Entities;

public class Transaction
{
    public int Id { get; set; }


    // 交易類型：支出、收入、轉帳
    public TransactionType Type { get; set; }


    // 金額
    public decimal Amount { get; set; }


    // 分類，例如：飲食、交通、薪資
    public string Category { get; set; } = string.Empty;


    // 備註
    public string Note { get; set; } = string.Empty;


    // 交易日期
    public DateTime TransactionDate { get; set; }


    // 使用哪個帳戶
    public int AccountId { get; set; }


    // 對應帳戶
    public Account Account { get; set; } = null!;


    // 轉帳目的帳戶
    // 如果不是轉帳，可以為 null
    public int? ToAccountId { get; set; }


    public Account? ToAccount { get; set; }


    public DateTime CreatedAtUtc { get; set; }
        = DateTime.UtcNow;


    public DateTime UpdatedAtUtc { get; set; }
        = DateTime.UtcNow;
}