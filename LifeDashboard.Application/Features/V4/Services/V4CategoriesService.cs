using LifeDashboard.Application.Features.V4.Dtos;
using LifeDashboard.Application.Features.V4.Interfaces;

namespace LifeDashboard.Application.Features.V4.Services;

public sealed class V4CategoriesService : IV4CategoriesService
{
    private readonly IV4CategoryReader _reader;

    public V4CategoriesService(IV4CategoryReader reader)
    {
        _reader = reader;
    }

    public async Task<IReadOnlyList<CategoryReadDto>> GetCategoriesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var projection = await _reader.GetCategoriesAsync(
            userId,
            cancellationToken);

        return projection
            .Select(category => new CategoryReadDto(
                category.Id,
                category.Name,
                category.Icon,
                category.BucketType,
                category.CreatedAt))
            .ToArray();
    }
}
