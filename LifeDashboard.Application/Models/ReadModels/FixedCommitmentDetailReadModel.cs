using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Application.Models.ReadModels;

public sealed record FixedCommitmentDetailReadModel(
    Guid Id,
    string Name,
    CommitmentType CommitmentType,
    decimal MonthlyAmount,
    DateOnly StartMonth,
    DateOnly? EndMonth);