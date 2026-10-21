using LifeDashboard.Domain.Entities;

namespace LifeDashboard.Application.Repositories.Interfaces;


// ============================================
// User Repository Interface
// 負責定義 User 資料存取方式
// Application 層只知道介面
// 不知道 SQL Server 或 EF Core
// 實作放 Infrastructure
// ============================================

public interface IUserRepository
{

    // 查詢帳號是否存在
    Task<bool> ExistsAsync(
        string username);


    // 依帳號取得 User
    Task<User?> GetByUsernameAsync(
        string username);


    // 新增 User
    Task AddAsync(
        User user);

}