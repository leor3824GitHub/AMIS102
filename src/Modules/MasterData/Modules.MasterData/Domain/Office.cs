using FSH.Framework.Core.Domain;

namespace FSH.Modules.MasterData.Domain;

public sealed class Office : AggregateRoot<Guid>, IAuditableEntity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public string? Address { get; private set; }
    /// <summary>NFA 4-digit location code (e.g. 8300, 0100, 1301).</summary>
    public string? LocationCode { get; private set; }
    /// <summary>NFA Regional/Provincial Code (e.g. 100, NDO, 00B). Null for Central Office units.</summary>
    public string? RegProvCode { get; private set; }
    public bool IsActive { get; private set; } = true;
    public byte[] Version { get; set; } = [];

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    public static Office Create(string code, string name, string? description, string? regProvCode = null, string? locationCode = null, string? address = null)
    {
        return new Office
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = description,
            Address = address,
            LocationCode = locationCode,
            RegProvCode = regProvCode,
            IsActive = true,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    public void Update(string code, string name, string? description, bool isActive, string? regProvCode = null, string? locationCode = null, string? address = null)
    {
        Code = code;
        Name = name;
        Description = description;
        Address = address;
        LocationCode = locationCode;
        RegProvCode = regProvCode;
        IsActive = isActive;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}

