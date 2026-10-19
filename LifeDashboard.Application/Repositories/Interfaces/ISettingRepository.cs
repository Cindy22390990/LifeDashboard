using LifeDashboard.Domain.Entities;

namespace LifeDashboard.Application.Repositories.Interfaces
{
    // 系統設定 Repository 介面
    // 負責定義「系統設定」對資料庫的存取合約，但不包含真正的 SQL 指令
    // 為什麼這樣設計：實現依賴反轉 (Dependency Inversion)。Application 層只需要知道「可以儲存和讀取」，不用管底層是 SQL Server 還是 SQLite，方便未來替換資料庫或做單元測試。
    // 如果要修改功能應該修改哪裡：如果未來需要新的資料庫查詢條件（例如：根據多個條件篩選設定），請在這裡新增方法定義。
    public interface ISettingRepository
    {
        // 取得使用者的設定 (預留 UserId 參數支援未來多帳號)
        Task<SystemSetting?> GetByUserIdAsync(Guid userId);

        // 新增設定 (第一次使用 APP 時)
        Task AddAsync(SystemSetting setting);

        // 更新設定
        Task UpdateAsync(SystemSetting setting);
    }
}