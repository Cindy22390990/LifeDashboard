using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LifeDashboard.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly LifeDashboardDbContext _context;

    public CategoryRepository(LifeDashboardDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Category>> GetAllAsync(Guid userId)
    {
        return await _context.Categories
            .Where(category => category.UserId == userId)
            .OrderBy(category => category.Name)
            .ToListAsync();
    }

    public async Task<Category?> GetByIdAsync(Guid id, Guid userId)
    {
        return await _context.Categories
            .FirstOrDefaultAsync(category =>
                category.Id == id && category.UserId == userId);
    }

    public async Task AddAsync(Category category)
    {
        await _context.Categories.AddAsync(category);
    }

    public Task RemoveAsync(Category category)
    {
        _context.Categories.Remove(category);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
