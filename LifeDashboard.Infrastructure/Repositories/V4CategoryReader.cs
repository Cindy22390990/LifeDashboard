using LifeDashboard.Application.Features.V4.Interfaces;
using LifeDashboard.Application.Features.V4.Models;
using LifeDashboard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LifeDashboard.Infrastructure.Repositories;

public sealed class V4CategoryReader : IV4CategoryReader
{
    private readonly LifeDashboardDbContext _context;

    public V4CategoryReader(LifeDashboardDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CategoryReadProjection>> GetCategoriesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _context.Categories
            .AsNoTracking()
            .Where(category => category.UserId == userId)
            .OrderBy(category => category.Name)
            .Select(category => new CategoryReadProjection(
                category.Id,
                category.Name,
                category.Icon,
                category.BucketType,
                category.CreatedAt))
            .ToArrayAsync(cancellationToken);
    }
}
