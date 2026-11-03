using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Domain.Entities;

public class FixedCommitment
{
    private FixedCommitment()
    {
    }

    public FixedCommitment(
        Guid userId,
        string name,
        decimal monthlyAmount,
        CommitmentType commitmentType,
        DateOnly startMonth,
        DateOnly? endMonth)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        UpdateDetails(name, monthlyAmount, commitmentType, startMonth, endMonth);
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal MonthlyAmount { get; private set; }
    public CommitmentType CommitmentType { get; private set; }
    public DateOnly StartMonth { get; private set; }
    public DateOnly? EndMonth { get; private set; }

    public void UpdateDetails(
        string name,
        decimal monthlyAmount,
        CommitmentType commitmentType,
        DateOnly startMonth,
        DateOnly? endMonth)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (monthlyAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(monthlyAmount),
                "每月金額必須大於 0。");
        }

        if (!Enum.IsDefined(commitmentType))
        {
            throw new ArgumentOutOfRangeException(nameof(commitmentType));
        }

        if (startMonth == default || startMonth.Day != 1)
        {
            throw new ArgumentException(
                "開始月份必須是有效月份的第一天。",
                nameof(startMonth));
        }

        if (endMonth is { } end && end.Day != 1)
        {
            throw new ArgumentException("結束月份必須為該月第一天。", nameof(endMonth));
        }

        if (endMonth is { } endDate && endDate < startMonth)
        {
            throw new ArgumentException("結束月份不可早於開始月份。", nameof(endMonth));
        }

        Name = name;
        MonthlyAmount = monthlyAmount;
        CommitmentType = commitmentType;
        StartMonth = startMonth;
        EndMonth = endMonth;
    }
}
