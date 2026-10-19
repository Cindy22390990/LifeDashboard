using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LifeDashboard.Infrastructure.Repositories
{
    // 系統設定 Repository 實作
    // 負責真正與資料庫 (SQL) 溝通，執行 CRUD 操作
    // 為什麼這樣設計：把 EF Core 的細節 (DbContext) 關在 Infrastructure 層，Application 層只要呼叫介面，不用管背後是不是 Entity Framework。
    // 如果要修改功能應該修改哪裡：如果未來要換成 Dapper 或直接寫 SQL，就是在這層修改，不會動到核心邏輯。
    public class SettingRepository : ISettingRepository
    {
        private readonly LifeDashboardDbContext _context;

        public SettingRepository(LifeDashboardDbContext context)
        {
            _context = context;
        }

        // 根據 UserId 取得系統設定
        public async Task<SystemSetting?> GetByUserIdAsync(Guid userId)
        {
            return await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.UserId == userId);
        }

        // 新增系統設定
        public async Task AddAsync(SystemSetting setting)
        {
            await _context.SystemSettings.AddAsync(setting);
            await _context.SaveChangesAsync();
        }

        // 更新系統設定
        public async Task UpdateAsync(SystemSetting setting)
        {
            _context.SystemSettings.Update(setting);
            await _context.SaveChangesAsync();
        }
    }
}