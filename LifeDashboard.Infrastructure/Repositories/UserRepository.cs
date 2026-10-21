using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;


namespace LifeDashboard.Infrastructure.Repositories;


// ============================================
// User Repository 實作
// 使用 EF Core 操作 Users Table
// 如果未來換資料庫:
// SQL Server → PostgreSQL
// 只需要修改這層
// ============================================

public class UserRepository : IUserRepository
{

    private readonly LifeDashboardDbContext _context;


    public UserRepository(
        LifeDashboardDbContext context)
    {
        _context = context;
    }



    public async Task<bool> ExistsAsync(
        string username)
    {

        return await _context.Users
            .AnyAsync(x =>
                x.Username == username);

    }



    public async Task<User?> GetByUsernameAsync(
        string username)
    {

        return await _context.Users
            .FirstOrDefaultAsync(x =>
                x.Username == username);

    }



    public async Task AddAsync(
        User user)
    {

        await _context.Users.AddAsync(user);

        await _context.SaveChangesAsync();

    }

}