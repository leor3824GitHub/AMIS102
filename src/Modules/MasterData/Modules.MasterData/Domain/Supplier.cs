using FSH.Framework.Core.Domain;

namespace FSH.Modules.MasterData.Domain;

public sealed class Supplier : AggregateRoot<Guid>, IAuditableEntity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? TinNo { get; private set; }
    public string BusinessTaxType { get; private set; } = "NON-VAT";
    public string? Description { get; private set; }
    public string? ContactPerson { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public bool IsActive { get; private set; } = true;
    public byte[] Version { get; set; } = [];

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    public static Supplier Create(string code, string name, string? tinNo, string businessTaxType, string? description, string? contactPerson, string? email, string? phone, string? address)
    {
        return new Supplier
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            TinNo = tinNo,
            BusinessTaxType = NormalizeBusinessTaxType(businessTaxType),
            Description = description,
            ContactPerson = contactPerson,
            Email = email,
            Phone = phone,
            Address = address,
            IsActive = true,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    public static Supplier Create(string code, string name, string? description, string? contactPerson, string? email, string? phone, string? address)
        => Create(code, name, null, "NON-VAT", description, contactPerson, email, phone, address);

    public void Update(string code, string name, string? tinNo, string businessTaxType, string? description, string? contactPerson, string? email, string? phone, string? address, bool isActive)
    {
        Code = code;
        Name = name;
        TinNo = tinNo;
        BusinessTaxType = NormalizeBusinessTaxType(businessTaxType);
        Description = description;
        ContactPerson = contactPerson;
        Email = email;
        Phone = phone;
        Address = address;
        IsActive = isActive;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Update(string code, string name, string? description, string? contactPerson, string? email, string? phone, string? address, bool isActive)
        => Update(code, name, null, "NON-VAT", description, contactPerson, email, phone, address, isActive);

    private static string NormalizeBusinessTaxType(string value)
    {
        if (string.Equals(value, "VAT", StringComparison.OrdinalIgnoreCase))
        {
            return "VAT";
        }

        if (string.Equals(value, "NON-VAT", StringComparison.OrdinalIgnoreCase))
        {
            return "NON-VAT";
        }

        return "NON-VAT";
    }
}
