using System;
using LifeDashboard.Domain.ValueObjects;

namespace LifeDashboard.Domain.Entities
{
    // 系統設定 Entity (Aggregate Root)
    // 負責管理使用者的個人化資訊與全域財務規則（如發薪日、分配比例）
    // 為什麼這樣設計：將使用者的全域設定獨立出來，不再和交易紀錄混在一起。這是系統設定模組的核心。
    // 如果要修改功能應該修改哪裡：如果要增加新的設定選項（例如：預設語系、UI主題），請在此實體新增屬性及對應的 Update 方法。
    public class SystemSetting
    {
        // 資料庫主鍵
        public Guid Id { get; private set; }

        // 預留欄位：未來若支援多使用者，可用此關聯 User
        public Guid UserId { get; private set; }

        // 稱呼 (例如：我)
        public string UserName { get; private set; } = string.Empty;

        // 密碼雜湊值，絕對不可存明碼
        public string PasswordHash { get; private set; } = string.Empty;

        // 發薪日期 (每月幾號)
        public int Payday { get; private set; }

        // 資產分配比例 (Value Object)
        public AllocationRatio Allocation { get; private set; } = null!;

        // 建立時間
        public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
        // 最後修改時間
        public DateTime UpdatedAtUtc { get; private set; } = DateTime.UtcNow;

        // 供 Entity Framework Core 使用的無參數建構子 (隱藏起來)
        protected SystemSetting() { }

        // 建立新設定時使用的建構子
        public SystemSetting(Guid userId, string userName, string passwordHash, int payday, AllocationRatio allocation)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            UserName = userName;
            PasswordHash = passwordHash;
            Allocation = allocation;

            // 呼叫下方的方法來確保發薪日規則一致
            ChangePayday(payday);
        }

        // 修改發薪日
        // 為什麼這樣設計：封裝屬性修改行為，確保外部不能隨意賦予不合理的日期 (如 32 號)
        public void ChangePayday(int newPayday)
        {
            if (newPayday < 1 || newPayday > 31)
            {
                throw new ArgumentException("發薪日必須在 1 到 31 號之間。");
            }

            Payday = newPayday;
            UpdatedAtUtc = DateTime.UtcNow;
        }

        // 修改分配比例
        // 為什麼這樣設計：透過傳入已經驗證過 100% 的 AllocationRatio 物件，確保此處接到的比例絕對安全
        public void UpdateAllocation(AllocationRatio newAllocation)
        {
            if (newAllocation == null)
            {
                throw new ArgumentNullException(nameof(newAllocation), "分配比例不可為空。");
            }

            Allocation = newAllocation;
            UpdatedAtUtc = DateTime.UtcNow;
        }

        // 修改稱呼
        public void UpdateUserName(string newUserName)
        {
            if (string.IsNullOrWhiteSpace(newUserName))
            {
                throw new ArgumentException("稱呼不可為空。");
            }
            UserName = newUserName;
            UpdatedAtUtc = DateTime.UtcNow;
        }
    }
}