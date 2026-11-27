using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LifeDashboard.Infrastructure.Repositories;

public sealed class UserPreferenceRepository : IUserPreferenceRepository
{
    private readonly LifeDashboardDbContext _context;

    public UserPreferenceRepository(LifeDashboardDbContext context)
    {
        _context = context;
    }

    public Task<UserPreference?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return _context.UserPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(
                preference => preference.UserId == userId,
                cancellationToken);
    }

    public async Task SaveAsync(
        UserPreference preference,
        CancellationToken cancellationToken)
    {
        var existing = await _context.UserPreferences
            .FirstOrDefaultAsync(
                item => item.UserId == preference.UserId,
                cancellationToken);

        if (existing is null)
        {
            await _context.UserPreferences.AddAsync(
                preference,
                cancellationToken);
        }
        else
        {
            existing.Update(
                preference.DefaultExpenseAccountId,
                preference.DefaultCategoryId);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
