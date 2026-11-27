using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Application.Features.V4.Dtos;

public sealed record CategoryReadDto(
    Guid Id,
    string Name,
    string Icon,
    BucketType BucketType,
    DateTime CreatedAt);
