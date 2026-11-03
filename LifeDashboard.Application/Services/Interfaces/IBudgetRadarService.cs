using LifeDashboard.Application.Models.DTOs;

namespace LifeDashboard.Application.Services.Interfaces;

public interface IBudgetRadarService
{
    Task<RadarStateDto> GetRadarStateAsync(Guid userId, DateOnly currentDate);
}
