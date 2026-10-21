using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using LifeDashboard.Application.Interfaces.Security;
using LifeDashboard.Domain.Entities;

using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;


namespace LifeDashboard.Infrastructure.Security;

// JWT Token 產生器
// 功能:
// 1. 讀取 appsettings JwtSettings
// 2. 建立 User 身份 Claim
// 3. 產生加密 Token
// 登入流程:
// Username + Password
//          ↓
// 驗證成功
//          ↓
// User Entity
//          ↓
// GenerateToken()
//          ↓
// JWT
public class JwtTokenGenerator : ITokenGenerator
{

    private readonly IConfiguration _configuration;


    public JwtTokenGenerator(
        IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // 建立 JWT Token
    // Token 內會保存:
    // - UserId
    // - Username
    public string GenerateToken(User user)
    {

        var jwtSettings =
            _configuration.GetSection("JwtSettings");


        var key =
            jwtSettings["SecretKey"]
            ?? throw new Exception("JWT SecretKey 未設定");


        var issuer =
            jwtSettings["Issuer"];


        var audience =
            jwtSettings["Audience"];



        // JWT Payload 內容
        var claims = new[]
        {
            // 後端取得 UserId 的主要來源
            new Claim(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()
            ),


            // 顯示使用者名稱
            new Claim(
                ClaimTypes.Name,
                user.Username
            )
        };



        var securityKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(key)
            );


        var credentials =
            new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256
            );



        var expireMinutes =
            int.Parse(
                jwtSettings["ExpireMinutes"] ?? "60"
            );



        var token =
            new JwtSecurityToken(

                issuer,

                audience,

                claims,

                expires:
                    DateTime.UtcNow.AddMinutes(
                        expireMinutes
                    ),

                signingCredentials:
                    credentials
            );



        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}