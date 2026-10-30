using System;
using System.Collections.Generic; // 確保有引入這個
using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Domain.Entities
{
    // 帳戶實體 (Entity)
    // 功能：系統管理資金流動的聚合根 (Aggregate Root)
    public class Account
    {
        public Guid Id { get; private set; }
        // 所屬使用者
        public Guid UserId { get; private set; }


        // 導覽屬性
        public virtual User User { get; private set; } = null!;
        public string Name { get; set; } = string.Empty;
        public AccountType Type { get; private set; }
        
        // 初始餘額 (精確度防呆設定將在 EF Core 設定中處理)
        public decimal InitialBalance{ get; set; }
        
        public decimal CurrentBalance { get; set; }
        // 系統帳戶標記：為 true 時前端將「隱藏刪除按鈕」，且後端禁止刪除
        public bool IsSystemAccount { get; private set; }
        
        // 軟刪除標記：預設為 false
        public bool IsDeleted { get; private set; }
        
        public DateTime CreatedAtUtc { get; private set; }
        public DateTime UpdatedAtUtc { get; private set; }

        public virtual ICollection<Transaction> Transactions { get; private set; } = new List<Transaction>();
        // 給 EF Core 建立物件用的無參數建構子
        private Account() { }

        // 建立新帳戶的工廠方法/建構子
        public Account(
            Guid userId,
            string name,
            AccountType type,
            decimal initialBalance,
            bool isSystemAccount = false)
        {
            Id = Guid.NewGuid();

            UserId = userId;

            Name = name;

            Type = type;

            InitialBalance = initialBalance;

            IsSystemAccount = isSystemAccount;

            IsDeleted = false;

            CreatedAtUtc = DateTime.UtcNow;

            UpdatedAtUtc = DateTime.UtcNow;
        }

        // 修改帳戶資訊的行為
        public void UpdateDetails(string name, AccountType type)
        {
            Name = name;
            Type = type;
            UpdatedAtUtc = DateTime.UtcNow;
        }

        // 軟刪除行為
        public void MarkAsDeleted()
        {
            // 領域層防呆：系統帳戶絕對不允許被刪除
            if (IsSystemAccount)
            {
                throw new InvalidOperationException("系統預設帳戶不可刪除。");
            }
            
            IsDeleted = true;
            UpdatedAtUtc = DateTime.UtcNow;
        }
    }
}