using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Domain.Entities;

public class Category
{
    private Category()
    {
    }

    public Category(Guid userId, string name, string icon, BucketType bucketType)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        UpdateDetails(name, icon, bucketType);
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Icon { get; private set; } = string.Empty;
    public BucketType BucketType { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public void UpdateDetails(string name, string icon, BucketType bucketType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(icon);

        if (name.Length > 50)
        {
            throw new ArgumentException("分類名稱不可超過 50 個字元。", nameof(name));
        }

        if (icon.Length > 10)
        {
            throw new ArgumentException("分類圖示不可超過 10 個字元。", nameof(icon));
        }

        if (!Enum.IsDefined(bucketType))
        {
            throw new ArgumentOutOfRangeException(nameof(bucketType));
        }

        Name = name;
        Icon = icon;
        BucketType = bucketType;
    }
}
