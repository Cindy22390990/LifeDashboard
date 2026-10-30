namespace LifeDashboard.Application.Models.DTOs.Auth;
// 登入資料
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;


    public string Password { get; set; } = string.Empty;
}