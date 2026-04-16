using FSH.Framework.Core.Domain;

namespace FSH.Modules.MasterData.Domain;

/// <summary>
/// Top-level COA GAM Annex A property classification (PPRCLSCD).
/// Shared reference data — not tenant-scoped.
/// Example: Code = "OE", Name = "Office Equipment"
/// </summary>
public sealed class PropertyClass : AggregateRoot<Guid>, IAuditableEntity
{
    /// <summary>2-character classification code used in property code generation (e.g. "OE", "TS", "LT").</summary>
    public string Code { get; private set; } = default!;

    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    public ICollection<PropertyClassItem> Items { get; private set; } = [];

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;

    private PropertyClass() { }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    public static PropertyClass Create(string code, string name, string? description)
    {
        return new PropertyClass
        {
            Id = Guid.NewGuid(),
            Code = code.ToUpperInvariant(),
            Name = name,
            Description = description,
            IsActive = true,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    public void Update(string name, string? description, bool isActive)
    {
        Name = name;
        Description = description;
        IsActive = isActive;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}
