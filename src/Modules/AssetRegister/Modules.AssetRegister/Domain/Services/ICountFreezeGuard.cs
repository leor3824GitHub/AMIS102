using AMIS.Modules.AssetRegister.Domain.Assets;

namespace AMIS.Modules.AssetRegister.Domain.Services;

/// <summary>
/// Enforces the physical-count ledger freeze: while a count session is active and frozen
/// (Ongoing or Reconciled with a freeze timestamp), asset movements covered by the session's
/// fund cluster and scope are blocked.
/// </summary>
public interface ICountFreezeGuard
{
    /// <summary>
    /// Throws a 409 Conflict <c>CustomException</c> when any of the given assets is covered
    /// by an active frozen count session. No-op when no freeze applies.
    /// </summary>
    Task EnsureMovementAllowedAsync(IReadOnlyCollection<AssetRegistry> assets, CancellationToken ct);
}
