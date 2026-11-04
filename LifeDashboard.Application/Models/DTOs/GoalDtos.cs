using System.ComponentModel.DataAnnotations;
using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Application.Models.DTOs;

public class GoalDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public GoalTier Tier { get; set; }
    public GoalType GoalType { get; set; }
    public GoalStatus Status { get; set; }
    public string Note { get; set; } = string.Empty;
    public DateTime? CompletedDate { get; set; }
}

public class CreateGoalDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public decimal TargetAmount { get; set; }

    [EnumDataType(typeof(GoalTier))]
    public GoalTier Tier { get; set; }

    [EnumDataType(typeof(GoalType))]
    public GoalType GoalType { get; set; }

    public string Note { get; set; } = string.Empty;
}

public class UpdateGoalDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public decimal TargetAmount { get; set; }

    [EnumDataType(typeof(GoalTier))]
    public GoalTier Tier { get; set; }

    [EnumDataType(typeof(GoalType))]
    public GoalType GoalType { get; set; }

    public string Note { get; set; } = string.Empty;
}
