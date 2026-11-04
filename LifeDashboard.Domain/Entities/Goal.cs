using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Domain.Entities;

public class Goal
{
    private Goal()
    {
    }

    public Goal(
        Guid userId,
        string name,
        decimal targetAmount,
        GoalTier tier,
        GoalType goalType,
        string note)
    {
        UserId = userId;
        CurrentAmount = 0;
        Status = GoalStatus.Active;
        CreatedAtUtc = DateTime.UtcNow;
        UpdateDetails(name, targetAmount, tier, goalType, note);
    }

    public int Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal TargetAmount { get; private set; }
    public decimal CurrentAmount { get; private set; }
    public GoalTier Tier { get; private set; }
    public GoalType GoalType { get; private set; }
    public GoalStatus Status { get; private set; }
    public string Note { get; private set; } = string.Empty;
    public DateTime? CompletedDate { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public void ApplyDeposit(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "存入金額必須大於 0。");
        }

        if (Status == GoalStatus.Completed)
        {
            throw new InvalidOperationException("已完成的目標不可再存入資金。");
        }

        CurrentAmount += amount;
        UpdatedAtUtc = DateTime.UtcNow;

        if (GoalType == GoalType.Saving && CurrentAmount >= TargetAmount)
        {
            Status = GoalStatus.Completed;
            CompletedDate = DateTime.UtcNow;
        }
    }

    public void CompletePurchase()
    {
        if (GoalType != GoalType.Purchase)
        {
            throw new InvalidOperationException("只有購物願望目標可以完成購買。");
        }

        if (Status != GoalStatus.Active)
        {
            throw new InvalidOperationException("目標已完成，不可再次購買。");
        }

        if (CurrentAmount < TargetAmount)
        {
            throw new InvalidOperationException("目標尚未達標，不可完成購買。");
        }

        Status = GoalStatus.Completed;
        CompletedDate = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateDetails(
        string name,
        decimal targetAmount,
        GoalTier tier,
        GoalType goalType,
        string note)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!Enum.IsDefined(tier))
        {
            throw new ArgumentOutOfRangeException(nameof(tier));
        }

        if (!Enum.IsDefined(goalType))
        {
            throw new ArgumentOutOfRangeException(nameof(goalType));
        }

        Name = name;
        TargetAmount = targetAmount;
        Tier = tier;
        GoalType = goalType;
        Note = note;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
