using System.Security.Cryptography;
using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.MasterData.Domain;

public sealed class OrganizationProfile : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? ShortName { get; private set; }
    public string? Address { get; private set; }
    public string? LogoUrl { get; private set; }

    /// <summary>3-character office code used in property code generation (e.g. "00B" for Caraga Regional Office).</summary>
    public string? AnnexECode { get; private set; }

    public byte[] Version { get; set; } = [];

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    public static OrganizationProfile Create(
        string tenantId,
        string name,
        string? shortName,
        string? address,
        string? logoUrl,
        string? annexECode = null)
    {
        return new OrganizationProfile
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            ShortName = shortName,
            Address = address,
            LogoUrl = logoUrl,
            AnnexECode = annexECode,
            CreatedOnUtc = DateTimeOffset.UtcNow,
            Version = NewVersion()
        };
    }

    public void Update(
        string name,
        string? shortName,
        string? address,
        string? logoUrl,
        string? annexECode = null)
    {
        Name = name;
        ShortName = shortName;
        Address = address;
        LogoUrl = logoUrl;
        AnnexECode = annexECode;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        Version = NewVersion();
    }

    private static byte[] NewVersion() => RandomNumberGenerator.GetBytes(8);
}

