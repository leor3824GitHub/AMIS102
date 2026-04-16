using FSH.Framework.Core.Domain;

namespace FSH.Modules.MasterData.Domain;

/// <summary>
/// COA GAM Annex A sub-category within a PropertyClass.
/// Represents the 1-character category code used in property code generation.
/// Shared reference data — not tenant-scoped.
/// Example: ClassCode = "OE", ItemCode = "C", Name = "Computer and Accessories"
/// </summary>
public sealed class PropertyClassItem : BaseEntity<Guid>, IAuditableEntity
{
    public Guid PropertyClassId { get; private set; }

    /// <summary>2-char classification code (denormalized from parent for query convenience).</summary>
    public string ClassCode { get; private set; } = default!;

    /// <summary>1-character category code used in property code generation (e.g. "C", "F", "O").</summary>
    public string ItemCode { get; private set; } = default!;

    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    public PropertyClass PropertyClass { get; private set; } = default!;

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;

    private PropertyClassItem() { }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    public static PropertyClassItem Create(
        Guid propertyClassId,
        string classCode,
        string itemCode,
        string name,
        string? description)
    {
        return new PropertyClassItem
        {
            Id = Guid.NewGuid(),
            PropertyClassId = propertyClassId,
            ClassCode = classCode.ToUpperInvariant(),
            ItemCode = itemCode.ToUpperInvariant(),
            Name = name,
            Description = description,
            IsActive = true,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    public void Update(string itemCode, string name, string? description, bool isActive)
    {
        ItemCode = itemCode.ToUpperInvariant();
        Name = name;
        Description = description;
        IsActive = isActive;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}
