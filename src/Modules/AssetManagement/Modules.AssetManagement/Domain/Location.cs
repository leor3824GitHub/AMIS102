using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.AssetManagement.Domain;

public sealed class Location : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public LocationType Type { get; private set; }
    public Guid? ParentLocationId { get; private set; }
    public string? Description { get; private set; }
    public byte[] Version { get; private set; } = [];

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }
    public bool IsDeleted { get; private set; }

    public static Location Create(
        string tenantId,
        string code,
        string name,
        LocationType type,
        Guid? parentLocationId,
        string? description)
    {
        return new Location
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code,
            Name = name,
            Type = type,
            ParentLocationId = parentLocationId,
            Description = description,
            CreatedOnUtc = DateTimeOffset.UtcNow,
        };
    }

    public void Update(string code, string name, LocationType type, Guid? parentLocationId, string? description)
    {
        Code = code;
        Name = name;
        Type = type;
        ParentLocationId = parentLocationId;
        Description = description;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void SoftDelete(string deletedBy)
    {
        IsDeleted = true;
        DeletedOnUtc = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
    }
}

