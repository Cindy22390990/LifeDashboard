namespace LifeDashboard.Application.Interfaces.Security;
// 密碼加密與驗證介面
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(
        string password,
        string passwordHash);
}