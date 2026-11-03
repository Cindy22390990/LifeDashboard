namespace LifeDashboard.Application.Models.DTOs;

public class RadarStateDto
{
    public decimal DailySafeLimit { get; set; }
    public decimal TodaySpent { get; set; }
    public decimal TodayRemaining { get; set; }
    public int RemainingDays { get; set; }
    public decimal TotalLivingBudget { get; set; }
    public decimal UncategorizedExpenseAmount { get; set; }
}
