using LifeDashboard.Domain.ValueObjects;

namespace LifeDashboard.Application.Models.ReadModels;

public sealed record BudgetRadarReadModel(
    BudgetSnapshot Snapshot,
    IReadOnlyList<FixedCommitmentDetailReadModel> ActiveCommitments);