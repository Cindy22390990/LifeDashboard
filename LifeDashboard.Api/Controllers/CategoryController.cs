using LifeDashboard.Api.Extensions;
using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Services.Interfaces;
using LifeDashboard.Application.Features.V4.Dtos;
using LifeDashboard.Application.Features.V4.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeDashboard.Api.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly IV4CategoriesService _v4CategoriesService;

    public CategoryController(
        ICategoryService categoryService,
        IV4CategoriesService v4CategoriesService)
    {
        _categoryService = categoryService;
        _v4CategoriesService = v4CategoriesService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryReadDto>>> GetCategories(
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        return Ok(await _v4CategoriesService.GetCategoriesAsync(
            userId,
            cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> CreateCategory(
        CreateCategoryDto dto)
    {
        var userId = User.GetUserId();
        var category = await _categoryService.CreateAsync(userId, dto);
        return StatusCode(StatusCodes.Status201Created, category);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateCategory(
        Guid id,
        UpdateCategoryDto dto)
    {
        try
        {
            var userId = User.GetUserId();
            await _categoryService.UpdateAsync(id, userId, dto);
            return NoContent();
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        try
        {
            var userId = User.GetUserId();
            await _categoryService.DeleteAsync(id, userId);
            return NoContent();
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }
}
