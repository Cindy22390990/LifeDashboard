namespace LifeDashboard.Application.Models.DTOs.Auth;

public class RegisterRequest
{
    // 使用者登入帳號
    public string Username { get; set; } = string.Empty;


    // 使用者輸入的明碼密碼
    // 注意:
    // 只存在 Request Memory
    // 不寫入資料庫
    public string Password { get; set; } = string.Empty;
}