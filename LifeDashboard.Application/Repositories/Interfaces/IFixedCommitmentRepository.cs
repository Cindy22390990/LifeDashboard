using LifeDashboard.Domain.Entities;

namespace LifeDashboard.Application.Repositories.Interfaces;

public interface IFixedCommitmentRepository
{
    Task<IEnumerable<FixedCommitment>> GetAllAsync(Guid userId);
    Task<IEnumerable<FixedCommitment>> GetActiveForPayrollPeriodAsync(
        Guid userId,
        DateOnly payrollPeriodMonth);
    Task<FixedCommitment?> GetByIdAsync(Guid id, Guid userId);
    Task AddAsync(FixedCommitment fixedCommitment);
    Task RemoveAsync(FixedCommitment fixedCommitment);
    Task SaveChangesAsync();
}
