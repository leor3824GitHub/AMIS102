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

    public Guid? RegionalManagerId { get; private set; }
    public string? RegionalManagerName { get; private set; }
    public string? RegionalManagerDesignation { get; private set; }
    public Guid? AssistantRegionalManagerId { get; private set; }
    public string? AssistantRegionalManagerName { get; private set; }
    public string? AssistantRegionalManagerDesignation { get; private set; }
    public Guid? AccountantId { get; private set; }
    public string? AccountantName { get; private set; }
    public string? AccountantDesignation { get; private set; }
    public Guid? SupervisingAdminOfficerId { get; private set; }
    public string? SupervisingAdminOfficerName { get; private set; }
    public string? SupervisingAdminOfficerDesignation { get; private set; }

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
        string? annexECode = null,
        Guid? regionalManagerId = null,
        string? regionalManagerName = null,
        string? regionalManagerDesignation = null,
        Guid? assistantRegionalManagerId = null,
        string? assistantRegionalManagerName = null,
        string? assistantRegionalManagerDesignation = null,
        Guid? accountantId = null,
        string? accountantName = null,
        string? accountantDesignation = null,
        Guid? supervisingAdminOfficerId = null,
        string? supervisingAdminOfficerName = null,
        string? supervisingAdminOfficerDesignation = null)
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
            RegionalManagerId = regionalManagerId,
            RegionalManagerName = regionalManagerName,
            RegionalManagerDesignation = regionalManagerDesignation,
            AssistantRegionalManagerId = assistantRegionalManagerId,
            AssistantRegionalManagerName = assistantRegionalManagerName,
            AssistantRegionalManagerDesignation = assistantRegionalManagerDesignation,
            AccountantId = accountantId,
            AccountantName = accountantName,
            AccountantDesignation = accountantDesignation,
            SupervisingAdminOfficerId = supervisingAdminOfficerId,
            SupervisingAdminOfficerName = supervisingAdminOfficerName,
            SupervisingAdminOfficerDesignation = supervisingAdminOfficerDesignation,
            CreatedOnUtc = DateTimeOffset.UtcNow,
            Version = NewVersion()
        };
    }

    public void Update(
        string name,
        string? shortName,
        string? address,
        string? logoUrl,
        string? annexECode = null,
        Guid? regionalManagerId = null,
        string? regionalManagerName = null,
        string? regionalManagerDesignation = null,
        Guid? assistantRegionalManagerId = null,
        string? assistantRegionalManagerName = null,
        string? assistantRegionalManagerDesignation = null,
        Guid? accountantId = null,
        string? accountantName = null,
        string? accountantDesignation = null,
        Guid? supervisingAdminOfficerId = null,
        string? supervisingAdminOfficerName = null,
        string? supervisingAdminOfficerDesignation = null)
    {
        Name = name;
        ShortName = shortName;
        Address = address;
        LogoUrl = logoUrl;
        AnnexECode = annexECode;
        RegionalManagerId = regionalManagerId;
        RegionalManagerName = regionalManagerName;
        RegionalManagerDesignation = regionalManagerDesignation;
        AssistantRegionalManagerId = assistantRegionalManagerId;
        AssistantRegionalManagerName = assistantRegionalManagerName;
        AssistantRegionalManagerDesignation = assistantRegionalManagerDesignation;
        AccountantId = accountantId;
        AccountantName = accountantName;
        AccountantDesignation = accountantDesignation;
        SupervisingAdminOfficerId = supervisingAdminOfficerId;
        SupervisingAdminOfficerName = supervisingAdminOfficerName;
        SupervisingAdminOfficerDesignation = supervisingAdminOfficerDesignation;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        Version = NewVersion();
    }

    private static byte[] NewVersion() => RandomNumberGenerator.GetBytes(8);
}

