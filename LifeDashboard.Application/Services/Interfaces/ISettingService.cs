using LifeDashboard.Application.Models.DTOs;

namespace LifeDashboard.Application.Services.Interfaces
{
    // 系統設定 Service 介面
    // 負責定義提供給 Controller (API 層) 呼叫的功能清單
    public interface ISettingService
    {
        Task<SystemSettingDto> GetSettingAsync(Guid userId);
        Task UpdateSettingAsync(Guid userId, SystemSettingDto dto);
        Task InitializeSettingAsync(Guid userId, SystemSettingDto dto);
    }
}