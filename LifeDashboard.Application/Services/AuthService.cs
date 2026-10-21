using LifeDashboard.Application.Interfaces.Security;
using LifeDashboard.Application.Models.DTOs.Auth;
using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Application.Services.Interfaces;
using LifeDashboard.Domain.Entities;


namespace LifeDashboard.Application.Services;


// ============================================
// Authentication Service
// 負責:
// 1. 建立 User
// 2. 驗證帳號密碼
// 3. 呼叫 JWT Token Generator
// 不負責:
// - HTTP Request
// - Controller 回傳格式
// 這些交給 API 層
// ============================================

public class AuthService : IAuthService
{

    private readonly IUserRepository _userRepository;

    private readonly IPasswordHasher _passwordHasher;

    private readonly ITokenGenerator _tokenGenerator;



    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    // ============================================
    // 使用者註冊
    // 流程:
    // Request
    // ↓
    // 檢查帳號
    // ↓
    // Password Hash
    // ↓
    // 建立 User Entity
    // ↓
    // 存入 Database
    //
    // ============================================
    public async Task RegisterAsync(
        RegisterRequest request)
    {

        var existingUser =
            await _userRepository
            .GetByUsernameAsync(
                request.Username
            );


        if(existingUser != null)
        {
            throw new Exception(
                "帳號已存在"
            );
        }



        var passwordHash =
            _passwordHasher.Hash(
                request.Password);



        var user = new User(
            request.Username,
            passwordHash);



        await _userRepository.AddAsync(user);

    }




    // ============================================
    // 使用者登入
    // 流程:
    // Username
    // ↓
    // 找 User
    // ↓
    // 驗證 Hash
    // ↓
    // 產生 JWT
    // ============================================
    public async Task<LoginResponse> LoginAsync(
    LoginRequest request)
    {

        var user =
            await _userRepository
                .GetByUsernameAsync(
                    request.Username);



        if(user == null)
        {
            throw new Exception(
                "帳號不存在");
        }



        var valid =
            _passwordHasher.Verify(
                request.Password,
                user.PasswordHash);



        if(!valid)
        {
            throw new Exception(
                "密碼錯誤");
        }

        var token =
            _tokenGenerator
            .GenerateToken(user);


         return new LoginResponse
        {
            Token = token,
            Username = user.Username
        };

    }

}