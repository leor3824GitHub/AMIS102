using AMIS.Modules.Multitenancy.Contracts.Dtos;

namespace AMIS.Modules.Multitenancy.Contracts;

/// <summary>
/// Reads and updates the single, global platform settings record.
/// Consumed across modules (e.g. Identity enforcement) via this contract only.
/// </summary>
public interface IPlatformSettingsService
{
    /// <summary>
    /// Gets the global platform settings. Falls back to defaults if none persisted yet.
    /// </summary>
    Task<PlatformSettingsDto> GetAsync(CancellationToken ct = default);

    /// <summary>
    /// Updates the global platform settings. Root tenant only.
    /// </summary>
    Task UpdateAsync(PlatformSettingsDto settings, CancellationToken ct = default);
}
