namespace AMIS.Modules.MasterData.Features.v1.CapitalizationThresholds;

internal static class CapitalizationThresholdCache
{
    // Tenant-scoped: under per-tenant databases each tenant has its own active threshold, so a global
    // key would serve one tenant's value to another.
    public static string ActiveKey(string? tenantId) => $"masterdata:capthreshold:active:{tenantId}";

    public static readonly TimeSpan ActiveTtl = TimeSpan.FromMinutes(15);
}
