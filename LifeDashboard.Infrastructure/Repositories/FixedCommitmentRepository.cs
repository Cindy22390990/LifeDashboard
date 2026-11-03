using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LifeDashboard.Infrastructure.Repositories;

public class FixedCommitmentRepository : IFixedCommitmentRepository
{
    private readonly LifeDashboardDbContext _context;

    public FixedCommitmentRepository(LifeDashboardDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<FixedCommitment>> GetAllAsync(Guid userId)
    {
        return await _context.FixedCommitments
            .Where(commitment => commitment.UserId == userId)
            .OrderBy(commitment => commitment.StartMonth)
            .ThenBy(commitment => commitment.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<FixedCommitment>> GetActiveForPayrollPeriodAsync(
        Guid userId,
        DateOnly payrollPeriodMonth)
    {
        return await _context.FixedCommitments
            .Where(commitment =>
                commitment.UserId == userId &&
                commitment.StartMonth <= payrollPeriodMonth &&
                (!commitment.EndMonth.HasValue ||
                 commitment.EndMonth.Value >= payrollPeriodMonth))
            .ToListAsync();
    }

    public async Task<FixedCommitment?> GetByIdAsync(Guid id, Guid userId)
    {
        return await _context.FixedCommitments
            .FirstOrDefaultAsync(commitment =>
                commitment.Id == id && commitment.UserId == userId);
    }

    public async Task AddAsync(FixedCommitment fixedCommitment)
    {
        await _context.FixedCommitments.AddAsync(fixedCommitment);
    }

    public Task RemoveAsync(FixedCommitment fixedCommitment)
    {
        _context.FixedCommitments.Remove(fixedCommitment);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
