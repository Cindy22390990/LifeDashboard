using System.ComponentModel.DataAnnotations;
using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Application.Models.DTOs;

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public BucketType BucketType { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCategoryDto
{
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    public string Icon { get; set; } = string.Empty;

    [EnumDataType(typeof(BucketType))]
    public BucketType BucketType { get; set; }
}

public class UpdateCategoryDto
{
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    public string Icon { get; set; } = string.Empty;

    [EnumDataType(typeof(BucketType))]
    public BucketType BucketType { get; set; }
}
