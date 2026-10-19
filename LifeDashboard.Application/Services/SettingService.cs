using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Application.Services.Interfaces;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Domain.ValueObjects;

namespace LifeDashboard.Application.Services
{
    // 系統設定 Service 實作
    // 負責協調整個更新設定的流程：接收 DTO -> 轉化為 Domain 物件 (觸發驗證) -> 呼叫 Repository 儲存
    // 為什麼這樣設計：把「商業流程」放在這裡，讓 Controller 變得很乾淨（Controller 只負責接 HTTP Request）。如果在 Controller 寫這段，架構就會變成義大利麵條。
    // 如果要修改功能應該修改哪裡：如果未來修改設定時需要額外做事情（例如：發送 Email 通知、或是寫入 Log 紀錄），就加在這個類別對應的方法裡。
    public class SettingService : ISettingService
    {
        private readonly ISettingRepository _settingRepository;

        public SettingService(ISettingRepository settingRepository)
        {
            _settingRepository = settingRepository;
        }

        // 取得設定
        public async Task<SystemSettingDto> GetSettingAsync(Guid userId)
        {
            var setting = await _settingRepository.GetByUserIdAsync(userId);

            if (setting == null)
            {
                // 如果找不到，看是要回傳預設值還是拋出 NotFound 例外
                throw new Exception("找不到該使用者的系統設定");
            }

            // 將 Entity 轉換為 DTO 交給前端
            return new SystemSettingDto
            {
                UserName = setting.UserName,
                Payday = setting.Payday,
                Living = setting.Allocation.Living,
                Savings = setting.Allocation.Savings,
                Investment = setting.Allocation.Investment,
                Entertainment = setting.Allocation.Entertainment
            };
        }

        // 更新設定
        public async Task UpdateSettingAsync(Guid userId, SystemSettingDto dto)
        {
            var setting = await _settingRepository.GetByUserIdAsync(userId);
            if (setting == null)
            {
                throw new Exception("找不到該使用者的系統設定");
            }

            // 1. 將前端傳來的數字，轉換為 Domain 的 Value Object
            // 這裡會觸發 AllocationRatio 裡面的 100% 防呆檢查！如果失敗會直接拋出 ArgumentException
            var newAllocation = new AllocationRatio(
                dto.Living,
                dto.Savings,
                dto.Investment,
                dto.Entertainment
            );

            // 2. 呼叫 Entity 的行為方法來修改屬性
            setting.UpdateUserName(dto.UserName);
            setting.ChangePayday(dto.Payday);
            setting.UpdateAllocation(newAllocation);

            // 3. 儲存回資料庫
            await _settingRepository.UpdateAsync(setting);
        }

        // 新增初始化設定方法
        public async Task InitializeSettingAsync(Guid userId, SystemSettingDto dto)
        {
            // 檢查是否已經存在設定，避免重複建立
            var existingSetting = await _settingRepository.GetByUserIdAsync(userId);
            if (existingSetting != null)
            {
                throw new InvalidOperationException("該使用者已經初始化過系統設定了，請使用更新 (PUT) 功能。");
            }

            // 1. 轉換為 AllocationRatio (這會觸發 100% 驗證)
            var newAllocation = new AllocationRatio(
                dto.Living,
                dto.Savings,
                dto.Investment,
                dto.Entertainment
            );

            // 2. 建立新的 SystemSetting 實體
            // 註：密碼因為目前還沒做認證模組，先給個假 Hash 確保符合 Entity 規則
            var newSetting = new SystemSetting(
                userId: userId,
                userName: dto.UserName,
                passwordHash: "TempHash_Please_Replace_Later",
                payday: dto.Payday,
                allocation: newAllocation
            );

            // 3. 存入資料庫
            await _settingRepository.AddAsync(newSetting);
        }
    }
}