using FSH.Framework.Core.Domain;

namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// Master catalog entry for a class of semi-expendable property (COA Circular 2022-004).
/// One SemiExpendableItem represents a type of item (e.g., "Laptop Computer");
/// individual physical units are tracked as <see cref="SemiExpendableProperty"/>.
/// </summary>
public sealed class SemiExpendableItem : AggregateRoot<Guid>, IAuditableEntity
{
    /// <summary>Short unique identifier assigned by supply division (e.g., "SEI-001").</summary>
    public string Code { get; private set; } = default!;

    /// <summary>Descriptive name of the item type (e.g., "Laptop Computer").</summary>
    public string Name { get; private set; } = default!;

    /// <summary>Brand, model, specification details.</summary>
    public string? Description { get; private set; }

    /// <summary>UACS object code per the Revised Chart of Accounts.</summary>
    public string? UACSObjectCode { get; private set; }

    /// <summary>Unit of measurement (e.g., Piece, Set, Unit).</summary>
    public string UnitOfMeasure { get; private set; } = default!;

    /// <summary>Estimated useful life in years per COA guidelines.</summary>
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

    public static SemiExpendableItem Create(
        string code,
        string name,
        string? description,
        string? uacsObjectCode,
        string unitOfMeasure,
        int? estimatedUsefulLifeYears)
    {
        return new SemiExpendableItem
        {
            Id = Guid.NewGuid(),
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
