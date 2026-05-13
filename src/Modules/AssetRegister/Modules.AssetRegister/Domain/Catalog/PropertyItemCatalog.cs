using FSH.Framework.Core.Domain;
using FSH.Modules.AssetRegister.Contracts.v1;

namespace FSH.Modules.AssetRegister.Domain.Catalog;

public sealed class PropertyItemCatalog : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public string DefaultPropertyClass { get; private set; } = default!;
    public string DefaultCategoryCode { get; private set; } = default!;
    public string DefaultUnit { get; private set; } = default!;
    public string? UacsObjectCode { get; private set; }
    public int EstimatedUsefulLifeYears { get; private set; }
    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    private PropertyItemCatalog() { }

    public static PropertyItemCatalog Create(
        string tenantId,
        string code,
        string description,
        string defaultPropertyClass,
        string defaultCategoryCode,
        string defaultUnit,
        string? uacsObjectCode,
        int estimatedUsefulLifeYears)
    {
        if (estimatedUsefulLifeYears <= 0)
            throw new InvalidOperationException("EstimatedUsefulLifeYears must be greater than zero.");

        return new PropertyItemCatalog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code,
            Description = description,
            DefaultPropertyClass = defaultPropertyClass,
            DefaultCategoryCode = defaultCategoryCode,
            DefaultUnit = defaultUnit,
            UacsObjectCode = uacsObjectCode,
            EstimatedUsefulLifeYears = estimatedUsefulLifeYears,
            IsActive = true,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    public void Update(
        string description,
        string defaultPropertyClass,
        string defaultCategoryCode,
        string defaultUnit,
        string? uacsObjectCode,
        int estimatedUsefulLifeYears)
    {
        if (estimatedUsefulLifeYears <= 0)
            throw new InvalidOperationException("EstimatedUsefulLifeYears must be greater than zero.");

        Description = description;
        DefaultPropertyClass = defaultPropertyClass;
        DefaultCategoryCode = defaultCategoryCode;
        DefaultUnit = defaultUnit;
        UacsObjectCode = uacsObjectCode;
        EstimatedUsefulLifeYears = estimatedUsefulLifeYears;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Reactivate()
    {
        IsActive = true;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}
