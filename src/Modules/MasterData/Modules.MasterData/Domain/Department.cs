using FSH.Framework.Core.Domain;

namespace FSH.Modules.MasterData.Domain;

public sealed class Department : AggregateRoot<Guid>, IAuditableEntity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public string? FundCluster { get; private set; }
    public string? OfficeCode { get; private set; }
    public bool IsActive { get; private set; } = true;
    public byte[] Version { get; set; } = [];

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    public static Department Create(string code, string name, string? description, string? fundCluster = null, string? officeCode = null)
    {
        return new Department
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = description,
            FundCluster = fundCluster,
            OfficeCode = officeCode,
            IsActive = true,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    public void Update(string code, string name, string? description, string? fundCluster, bool isActive)
    {
        Code = code;
        Name = name;
        Description = description;
        FundCluster = fundCluster;
        IsActive = isActive;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}

