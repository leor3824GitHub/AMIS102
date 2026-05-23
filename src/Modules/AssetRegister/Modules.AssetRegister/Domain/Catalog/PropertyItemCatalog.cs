using AMIS.Framework.Core.Domain;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Catalog;

namespace AMIS.Modules.AssetRegister.Domain.Catalog;

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

    /// <summary>
    /// Lifecycle status. New rows created inline from a PR start as <see cref="CatalogItemStatus.Draft"/>
    /// (no UACS yet); an Accountant's certification back-fills UACS and promotes the row to Ready.
    /// </summary>
    public CatalogItemStatus Status { get; private set; }

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
            Status = string.IsNullOrWhiteSpace(uacsObjectCode) ? CatalogItemStatus.Draft : CatalogItemStatus.Ready,
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

        // Promote Draft → Ready when admin adds UACS via Update. Never demote.
        if (Status == CatalogItemStatus.Draft && !string.IsNullOrWhiteSpace(uacsObjectCode))
            Status = CatalogItemStatus.Ready;
    }

    /// <summary>
    /// Idempotent back-fill of UACS triggered by an Accountant's "Funds Available" certification on a PR
    /// that references this catalog item. No-op when the row is already <see cref="CatalogItemStatus.Ready"/>
    /// or already carries a UACS code (the existing value wins; admin edits are authoritative).
    /// </summary>
    public bool BackfillUacs(string uacsObjectCode)
    {
        if (string.IsNullOrWhiteSpace(uacsObjectCode))
            return false;
        if (Status == CatalogItemStatus.Ready || !string.IsNullOrWhiteSpace(UacsObjectCode))
            return false;

        UacsObjectCode = uacsObjectCode.Trim();
        Status = CatalogItemStatus.Ready;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        return true;
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

