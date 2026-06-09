using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.Expendable.Domain.Products;

/// <summary>
/// A single employee's star rating (1-5) for a product. One row per (tenant, product, rater);
/// re-rating updates the existing row rather than creating a new one.
/// </summary>
public class ProductRating : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public const int MinValue = 1;
    public const int MaxValue = 5;

    public string TenantId { get; private set; } = default!;
    public Guid ProductId { get; private set; }

    /// <summary>Identity user id of the rater (one rating per user per product).</summary>
    public string RaterUserId { get; private set; } = default!;

    public int Value { get; private set; }

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    // ISoftDeletable
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    private ProductRating() { }

    public static ProductRating Create(string tenantId, Guid productId, string raterUserId, int value)
    {
        if (string.IsNullOrWhiteSpace(raterUserId))
            throw new InvalidOperationException("Rater user id is required.");

        return new ProductRating
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductId = productId,
            RaterUserId = raterUserId,
            Value = Clamp(value),
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    public void UpdateValue(int value)
    {
        Value = Clamp(value);
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    private static int Clamp(int value) => Math.Clamp(value, MinValue, MaxValue);
}
