using LifeDashboard.Domain.Enums;


namespace LifeDashboard.Domain.Entities;


public class Goal
{
    public int Id { get; set; }



    // 目標名稱
    // 例如：
    // Canon EOS R50
    // 緊急預備金
    public string Name { get; set; }
        = string.Empty;



    // 目標總金額
    public decimal TargetAmount { get; set; }



    // 已完成金額
    public decimal CurrentAmount { get; set; }



    // 目標類型
    // Saving / Purchase
    public GoalType Type { get; set; }



    // 金字塔層級
    public GoalTier Tier { get; set; }



    // 備註
    public string Note { get; set; }
        = string.Empty;



    // 是否完成
    public bool IsCompleted { get; set; }
        = false;



    public DateTime CreatedAtUtc { get; set; }
        = DateTime.UtcNow;



    public DateTime UpdatedAtUtc { get; set; }
        = DateTime.UtcNow;
}