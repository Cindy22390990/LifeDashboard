namespace LifeDashboard.Application.Models.DTOs;

public class MonthlyReportDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal Balance { get; set; }
    public List<CategoryBreakdownDto> Categories { get; set; } = [];
}
