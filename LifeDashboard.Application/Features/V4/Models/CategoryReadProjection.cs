using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Application.Features.V4.Models;

public sealed record CategoryReadProjection(
    Guid Id,
    string Name,
    string Icon,
    BucketType BucketType,
    DateTime CreatedAt);
