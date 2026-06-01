using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.Multitenancy.Domain;

/// <summary>
/// Global platform-wide settings. Single row (see <see cref="SingletonId"/>) managed by the root tenant.
/// Not tenant-scoped — the same policy applies uniformly to every tenant.
/// </summary>
public class PlatformSettings : BaseEntity<Guid>, IAuditableEntity
{
    /// <summary>Well-known fixed identifier for the one-and-only settings row.</summary>
    public static readonly Guid SingletonId = new("a1b2c3d4-0000-4000-8000-000000000001");

    // Session
    public int? MaxSessionsPerUser { get; set; }
    public int? IdleTimeoutMinutes { get; set; }
    public int AbsoluteTimeoutDays { get; set; } = 7;

    // Quotas (uniform per-tenant caps)
    public int? MaxUsersPerTenant { get; set; }
    public long? StorageLimitMb { get; set; }
    public int? ApiRateLimitPerMinute { get; set; }

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; private set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? LastModifiedOnUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }

    private PlatformSettings() { } // EF Core

    public static PlatformSettings CreateDefault(string? createdBy = null)
    {
        return new PlatformSettings
        {
            Id = SingletonId,
            CreatedBy = createdBy,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    public void Update(string? modifiedBy)
    {
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    public void ResetToDefaults()
    {
        MaxSessionsPerUser = null;
        IdleTimeoutMinutes = null;
        AbsoluteTimeoutDays = 7;
        MaxUsersPerTenant = null;
        StorageLimitMb = null;
        ApiRateLimitPerMinute = null;
    }
}
