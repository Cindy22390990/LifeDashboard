using System.ComponentModel.DataAnnotations;
using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Application.Models.DTOs;

public class FixedCommitmentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal MonthlyAmount { get; set; }
    public CommitmentType CommitmentType { get; set; }
    public DateOnly StartMonth { get; set; }
    public DateOnly? EndMonth { get; set; }
}

public class CreateFixedCommitmentDto : IValidatableObject
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public decimal MonthlyAmount { get; set; }

    [EnumDataType(typeof(CommitmentType))]
    public CommitmentType CommitmentType { get; set; }

    public DateOnly StartMonth { get; set; }
    public DateOnly? EndMonth { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MonthlyAmount <= 0)
        {
            yield return new ValidationResult(
                "每月金額必須大於 0。",
                [nameof(MonthlyAmount)]);
        }

        if (StartMonth == default)
        {
            yield return new ValidationResult(
                "開始月份不可為空，且必須是有效月份的第一天。",
                [nameof(StartMonth)]);
        }
    }
}

public class UpdateFixedCommitmentDto : IValidatableObject
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public decimal MonthlyAmount { get; set; }

    [EnumDataType(typeof(CommitmentType))]
    public CommitmentType CommitmentType { get; set; }

    public DateOnly StartMonth { get; set; }
    public DateOnly? EndMonth { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MonthlyAmount <= 0)
        {
            yield return new ValidationResult(
                "每月金額必須大於 0。",
                [nameof(MonthlyAmount)]);
        }

        if (StartMonth == default)
        {
            yield return new ValidationResult(
                "開始月份不可為空，且必須是有效月份的第一天。",
                [nameof(StartMonth)]);
        }
    }
}
