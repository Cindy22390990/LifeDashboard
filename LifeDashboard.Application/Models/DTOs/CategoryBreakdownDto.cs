using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Application.Models.DTOs;

public class CategoryBreakdownDto
{
    public Guid? CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public BucketType? BucketType { get; set; }
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public int TransactionCount { get; set; }
}
