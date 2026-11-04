using System.ComponentModel.DataAnnotations;

namespace LifeDashboard.Application.Models.DTOs;

/// <summary>Request to complete a purchase goal with an expense transaction.</summary>
public sealed record GoalPurchaseRequest
{
    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public required decimal ActualAmount { get; init; }

    public required Guid AccountId { get; init; }

    public required Guid CategoryId { get; init; }

    public string Note { get; init; } = string.Empty;
}
