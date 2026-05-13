using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.AssetManagement.Domain;

/// <summary>
/// Unified master catalog entry for any trackable property — both semi-expendable and PPE.
/// Whether a physical unit becomes SE or PPE is determined at receipt time by comparing
/// its acquisition cost against the active <see cref="CapitalizationThreshold"/>.
/// One PropertyItemCatalog represents a *type* of item (e.g., "Laptop Computer");
/// individual physical units are tracked as <see cref="SemiExpendableProperty"/> (SE) or
/// <see cref="PPEItem"/> (PPE).
/// </summary>
public sealed class PropertyItemCatalog : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;

    /// <summary>Short unique identifier assigned by the supply division (e.g., "LAP-001").</summary>
    public string Code { get; private set; } = default!;

    /// <summary>Descriptive name of the item type (e.g., "Laptop Computer").</summary>
    public string Name { get; private set; } = default!;

    /// <summary>Brand, model, or specification details.</summary>
    public string? Description { get; private set; }

    /// <summary>UACS object code per the Revised Chart of Accounts.</summary>
    public string? UACSObjectCode { get; private set; }

    /// <summary>Unit of measurement (e.g., Piece, Set, Unit).</summary>
    public string UnitOfMeasure { get; private set; } = default!;

    /// <summary>Estimated useful life in years per COA guidelines (relevant for PPE depreciation).</summary>
    public int? EstimatedUsefulLifeYears { get; private set; }

    public bool IsActive { get; private set; } = true;
    public byte[] Version { get; set; } = [];

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    public static PropertyItemCatalog Create(
        string tenantId,
        string code,
        string name,
        string? description,
        string? uacsObjectCode,
        string unitOfMeasure,
        int? estimatedUsefulLifeYears)
    {
        return new PropertyItemCatalog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code,
            Name = name,
            Description = description,
            UACSObjectCode = uacsObjectCode,
            UnitOfMeasure = unitOfMeasure,
            EstimatedUsefulLifeYears = estimatedUsefulLifeYears,
            IsActive = true,
            CreatedOnUtc = DateTimeOffset.UtcNow,
        };
    }

    public void Update(
        string code,
        string name,
        string? description,
        string? uacsObjectCode,
        string unitOfMeasure,
        int? estimatedUsefulLifeYears,
        bool isActive)
    {
        Code = code;
        Name = name;
        Description = description;
        UACSObjectCode = uacsObjectCode;
        UnitOfMeasure = unitOfMeasure;
        EstimatedUsefulLifeYears = estimatedUsefulLifeYears;
        IsActive = isActive;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}

