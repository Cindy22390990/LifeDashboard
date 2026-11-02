using LifeDashboard.Application.Models.DTOs;

namespace LifeDashboard.Application.Services.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetAllAsync(Guid userId);
    Task<CategoryDto> CreateAsync(Guid userId, CreateCategoryDto dto);
    Task UpdateAsync(Guid id, Guid userId, UpdateCategoryDto dto);
    Task DeleteAsync(Guid id, Guid userId);
}
