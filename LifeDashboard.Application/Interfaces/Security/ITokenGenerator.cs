using LifeDashboard.Domain.Entities;

namespace LifeDashboard.Application.Interfaces.Security;

// JWT Token 產生器介面
// Application 只知道:
// 我要 Token
// 不知道:
// Token 怎麼產生
// SecretKey 放哪
// 使用哪種加密方式
// 實作放 Infrastructure
public interface ITokenGenerator
{
    // 根據登入成功的 User
    // 產生 JWT 字串
    string GenerateToken(User user);
}