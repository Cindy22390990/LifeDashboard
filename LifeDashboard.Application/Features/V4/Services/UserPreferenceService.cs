using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Domain.Entities;

namespace LifeDashboard.Application.Features.V4.Services;

public sealed class UserPreferenceService : IUserPreferenceService
{
    private readonly IUserPreferenceRepository _preferenceRepository;
    private readonly UserPreferenceValidator _validator;

    public UserPreferenceService(
        IUserPreferenceRepository preferenceRepository,
        UserPreferenceValidator validator)
    {
        _preferenceRepository = preferenceRepository;
        _validator = validator;
    }

    public async Task SaveAsync(
        Guid userId,
        Guid? defaultExpenseAccountId,
        Guid? defaultCategoryId,
        CancellationToken cancellationToken)
    {
        await _validator.ValidateAsync(
            userId,
            defaultExpenseAccountId,
            defaultCategoryId,
            cancellationToken);

        var preference = await _preferenceRepository.GetByUserIdAsync(
            userId,
            cancellationToken);

        if (preference is null)
        {
            preference = new UserPreference(
                userId,
                defaultExpenseAccountId,
                defaultCategoryId);
        }
        else
        {
            preference.Update(
                defaultExpenseAccountId,
                defaultCategoryId);
        }

        await _preferenceRepository.SaveAsync(
            preference,
            cancellationToken);
    }
}
