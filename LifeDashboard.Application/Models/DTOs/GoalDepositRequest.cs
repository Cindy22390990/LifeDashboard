using System.ComponentModel.DataAnnotations;

namespace LifeDashboard.Application.Models.DTOs;

/// <summary>Request to deposit funds into a goal by transferring between accounts.</summary>
public sealed record GoalDepositRequest
{
    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public required decimal Amount { get; init; }

    public required Guid FromAccountId { get; init; }

    public required Guid ToAccountId { get; init; }

    public string Note { get; init; } = string.Empty;
}
