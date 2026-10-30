namespace LifeDashboard.Domain.Entities;

public class User
{

    public User(
        string username,
        string passwordHash)
    {
        Id = Guid.NewGuid();
        Username = username;
        PasswordHash = passwordHash;
    }
    public Guid Id { get; set; }

    // 登入帳號
    public string Username { get; set; } = string.Empty;


    // BCrypt Hash 後的密碼
    public string PasswordHash { get; set; } = string.Empty;


    public DateTime CreatedAt { get; set; }
        = DateTime.UtcNow;


    // 一個 User 擁有多個帳戶
    public ICollection<Account> Accounts { get; set; }
        = new List<Account>();


    // 一個 User 擁有多筆交易
    public ICollection<Transaction> Transactions { get; set; }
        = new List<Transaction>();


    // 一個 User 一份設定
    public SystemSetting? SystemSetting { get; set; }



}