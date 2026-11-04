using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LifeDashboard.Infrastructure.Repositories;

public class GoalRepository : IGoalRepository
{
    private readonly LifeDashboardDbContext _context;

    public GoalRepository(LifeDashboardDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Goal>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _context.Goals
            .AsNoTracking()
            .Where(goal => goal.UserId == userId)
            .OrderBy(goal => goal.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<Goal?> GetByIdAsync(
        int id,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return _context.Goals.FirstOrDefaultAsync(
            goal => goal.Id == id && goal.UserId == userId,
            cancellationToken);
    }

    public async Task AddAsync(Goal goal, CancellationToken cancellationToken)
    {
        await _context.Goals.AddAsync(goal, cancellationToken);
    }

    public void Remove(Goal goal)
    {
        _context.Goals.Remove(goal);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
