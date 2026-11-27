namespace LifeDashboard.Domain.Entities;

public sealed class UserPreference
{
    private UserPreference()
    {
    }

    public UserPreference(
        Guid userId,
        Guid? defaultExpenseAccountId = null,
        Guid? defaultCategoryId = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        DefaultExpenseAccountId = defaultExpenseAccountId;
        DefaultCategoryId = defaultCategoryId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? DefaultExpenseAccountId { get; private set; }
    public Guid? DefaultCategoryId { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public void Update(
        Guid? defaultExpenseAccountId,
        Guid? defaultCategoryId)
    {
        DefaultExpenseAccountId = defaultExpenseAccountId;
        DefaultCategoryId = defaultCategoryId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
