using LifeDashboard.Application.Models.DTOs.Auth;
using LifeDashboard.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;


namespace LifeDashboard.Api.Controllers
{
    // ============================================
    // Authentication Controller
    //
    // 負責:
    // 1. 使用者註冊
    // 2. 使用者登入
    // 未來維護:
    // 如果新增 Google Login / Apple Login
    // 也會擴充這個模組
    // ============================================
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {

        private readonly IAuthService _authService;


        // ============================================
        // 建構子 Dependency Injection
        //
        // 從 Program.cs 注入 AuthService
        //
        // Controller 不直接建立 Service
        // 避免耦合
        // ============================================
        public AuthController(
            IAuthService authService)
        {
            _authService = authService;
        }



        // ============================================
        // 使用者註冊
        // POST:
        // /api/auth/register
        // 接收:
        // Username
        // Password
        // Password:
        // 進入後端後立即 Hash
        // 不保存明碼
        // ============================================
        [HttpPost("register")]
        public async Task<IActionResult> Register(
            RegisterRequest request)
        {
            await _authService.RegisterAsync(request);


            return Ok(new
            {
                message = "註冊成功"
            });
        }

        // ============================================
        // 使用者登入
        // POST:
        // /api/auth/login
        // 成功:
        // 回傳 JWT Token
        // 前端之後每次 API Request:
        // Authorization:
        // Bearer Token
        // ============================================
        [HttpPost("login")]
        public async Task<IActionResult> Login(
            LoginRequest request)
        {

           var response =
                await _authService.LoginAsync(request);

            return Ok(response);
        }

    }
}