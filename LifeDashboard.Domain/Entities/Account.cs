using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Domain.Entities;

public class Account
{
    //資料庫主鍵
    public int Id { get; set; }
    //帳戶名稱
    public string Name { get; set; } = string.Empty;
    //目前餘額
    public decimal Balance { get; set; }
    //日常 / 儲蓄 / 投資
    public AccountType Type { get; set; }
    //是否啟用，避免真的刪除資料
    public bool IsActive { get; set; } = true;
    //建立時間
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    //最後修改時間
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    //有哪些交易
    public ICollection<Transaction> Transactions { get; set; }
    = new List<Transaction>();
}