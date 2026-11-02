using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Application.Services.Interfaces;
using LifeDashboard.Domain.Entities;

namespace LifeDashboard.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<IEnumerable<CategoryDto>> GetAllAsync(Guid userId)
    {
        var categories = await _categoryRepository.GetAllAsync(userId);
        return categories.Select(ToDto);
    }

    public async Task<CategoryDto> CreateAsync(Guid userId, CreateCategoryDto dto)
    {
        var category = new Category(userId, dto.Name, dto.Icon, dto.BucketType);
        await _categoryRepository.AddAsync(category);
        await _categoryRepository.SaveChangesAsync();
        return ToDto(category);
    }

    public async Task UpdateAsync(Guid id, Guid userId, UpdateCategoryDto dto)
    {
        var category = await GetOwnedCategoryAsync(id, userId);
        category.UpdateDetails(dto.Name, dto.Icon, dto.BucketType);
        await _categoryRepository.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var category = await GetOwnedCategoryAsync(id, userId);
        await _categoryRepository.RemoveAsync(category);
        await _categoryRepository.SaveChangesAsync();
    }

    private async Task<Category> GetOwnedCategoryAsync(Guid id, Guid userId)
    {
        return await _categoryRepository.GetByIdAsync(id, userId)
            ?? throw new KeyNotFoundException("找不到指定的分類。");
    }

    private static CategoryDto ToDto(Category category)
    {
        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Icon = category.Icon,
            BucketType = category.BucketType,
            CreatedAt = category.CreatedAt
        };
    }
}
