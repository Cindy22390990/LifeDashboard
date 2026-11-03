using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Application.Services.Interfaces;
using LifeDashboard.Domain.Entities;

namespace LifeDashboard.Application.Services;

public class FixedCommitmentService : IFixedCommitmentService
{
    private readonly IFixedCommitmentRepository _fixedCommitmentRepository;

    public FixedCommitmentService(IFixedCommitmentRepository fixedCommitmentRepository)
    {
        _fixedCommitmentRepository = fixedCommitmentRepository;
    }

    public async Task<IEnumerable<FixedCommitmentDto>> GetAllAsync(Guid userId)
    {
        var commitments = await _fixedCommitmentRepository.GetAllAsync(userId);
        return commitments.Select(ToDto);
    }

    public async Task<FixedCommitmentDto> CreateAsync(
        Guid userId,
        CreateFixedCommitmentDto dto)
    {
        var commitment = new FixedCommitment(
            userId,
            dto.Name,
            dto.MonthlyAmount,
            dto.CommitmentType,
            dto.StartMonth,
            dto.EndMonth);

        await _fixedCommitmentRepository.AddAsync(commitment);
        await _fixedCommitmentRepository.SaveChangesAsync();
        return ToDto(commitment);
    }

    public async Task UpdateAsync(
        Guid id,
        Guid userId,
        UpdateFixedCommitmentDto dto)
    {
        var commitment = await GetOwnedCommitmentAsync(id, userId);
        commitment.UpdateDetails(
            dto.Name,
            dto.MonthlyAmount,
            dto.CommitmentType,
            dto.StartMonth,
            dto.EndMonth);

        await _fixedCommitmentRepository.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var commitment = await GetOwnedCommitmentAsync(id, userId);
        await _fixedCommitmentRepository.RemoveAsync(commitment);
        await _fixedCommitmentRepository.SaveChangesAsync();
    }

    private async Task<FixedCommitment> GetOwnedCommitmentAsync(Guid id, Guid userId)
    {
        return await _fixedCommitmentRepository.GetByIdAsync(id, userId)
            ?? throw new KeyNotFoundException("找不到指定的固定責任。");
    }

    private static FixedCommitmentDto ToDto(FixedCommitment commitment)
    {
        return new FixedCommitmentDto
        {
            Id = commitment.Id,
            Name = commitment.Name,
            MonthlyAmount = commitment.MonthlyAmount,
            CommitmentType = commitment.CommitmentType,
            StartMonth = commitment.StartMonth,
            EndMonth = commitment.EndMonth
        };
    }
}
