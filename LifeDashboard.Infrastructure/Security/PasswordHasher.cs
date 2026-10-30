using LifeDashboard.Application.Interfaces.Security;

namespace LifeDashboard.Infrastructure.Security;
// 負責:
// 1. 密碼 Hash
// 2. 密碼驗證

public class PasswordHasher : IPasswordHasher
{

    /// <summary>
    /// 建立密碼 Hash
    /// </summary>
    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }


    public bool Verify(
        string password,
        string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(
            password,
            passwordHash);
    }
}