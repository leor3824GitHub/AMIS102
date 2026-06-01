namespace AMIS.Modules.Multitenancy.Contracts.Dtos;

/// <summary>
/// Global, platform-wide settings configured by the root tenant.
/// A single record applies uniformly to every tenant.
/// </summary>
public sealed record PlatformSettingsDto
{
    public SessionSettingsDto Session { get; init; } = new();
    public QuotaSettingsDto Quota { get; init; } = new();

    public static PlatformSettingsDto Default => new();
}

public sealed record SessionSettingsDto
{
    /// <summary>Maximum concurrent active sessions per user. Null = unlimited.</summary>
    public int? MaxSessionsPerUser { get; init; }

    /// <summary>Session idle timeout in minutes. Null = no idle timeout.</summary>
    public int? IdleTimeoutMinutes { get; init; }

    /// <summary>Absolute session lifetime in days. Caps the refresh-token lifetime.</summary>
    public int AbsoluteTimeoutDays { get; init; } = 7;
}

public sealed record QuotaSettingsDto
{
    /// <summary>Maximum users allowed per tenant. Null = unlimited.</summary>
    public int? MaxUsersPerTenant { get; init; }

    /// <summary>Storage limit per tenant in megabytes. Null = unlimited. (Display-only for now.)</summary>
    public long? StorageLimitMb { get; init; }

    /// <summary>API request rate limit per minute, per tenant. Null = no additional limit.</summary>
    public int? ApiRateLimitPerMinute { get; init; }
}
