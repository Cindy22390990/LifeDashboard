namespace LifeDashboard.Application.Models.DTOs.Auth;
// 登入成功後 API 回傳資料
// 前端收到 Token 後：
// 1. 儲存在 Auth Store
// 2. 每次 API Request 放入 Authorization Header
public class LoginResponse
{

    // JWT Token
    public string Token { get; set; } = string.Empty;

    // 顯示於前端 UI 的使用者名稱
    // 例如:
    // 歡迎回來 Cindy

    public string Username { get; set; } = string.Empty;
}