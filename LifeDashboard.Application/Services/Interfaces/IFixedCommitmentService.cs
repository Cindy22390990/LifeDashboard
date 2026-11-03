using LifeDashboard.Application.Models.DTOs;

namespace LifeDashboard.Application.Services.Interfaces;

public interface IFixedCommitmentService
{
    Task<IEnumerable<FixedCommitmentDto>> GetAllAsync(Guid userId);
    Task<FixedCommitmentDto> CreateAsync(Guid userId, CreateFixedCommitmentDto dto);
    Task UpdateAsync(Guid id, Guid userId, UpdateFixedCommitmentDto dto);
    Task DeleteAsync(Guid id, Guid userId);
}
