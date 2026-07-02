using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.MasterData.Domain;

/// <summary>
/// National COA fund cluster (UACS funding-source rollup). A small, fixed controlled list
/// (01–07 per COA GAM) shared across all tenants. Consumed by AssetRegister ICS/PAR headers
/// and Procurement Planning. Seeded from <c>MasterDataDbInitializer</c>; editable via CRUD.
/// </summary>
public sealed class FundCluster : AggregateRoot<Guid>, IAuditableEntity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;
    public byte[] Version { get; private set; } = [];

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }
    public bool IsDeleted { get; private set; }

    public static FundCluster Create(string code, string name, string? description = null)
    {
        return new FundCluster
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = description,
            IsActive = true,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    public void Update(string code, string name, string? description, bool isActive)
    {
        Code = code;
        Name = name;
        Description = description;
        IsActive = isActive;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void SoftDelete(string deletedBy)
    {
        IsDeleted = true;
        DeletedOnUtc = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
    }
}
