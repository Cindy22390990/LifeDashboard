using LifeDashboard.Application.Models.DTOs.Auth;

namespace LifeDashboard.Application.Services.Interfaces;

// ============================================
// Authentication Service Interface
// 負責定義登入與註冊需要提供的功能
// Controller 只依賴這個介面
// 不知道底層怎麼實作
// 未來如果更換登入方式:
// JWT → Identity Server
// 密碼登入 → Apple Login
// 只需要替換 Service 實作
// ============================================

public interface IAuthService
{

    // 使用者註冊
    Task RegisterAsync(RegisterRequest request);


    // 使用者登入
    // 成功回傳 JWT Token
    Task<LoginResponse> LoginAsync(LoginRequest request);
}