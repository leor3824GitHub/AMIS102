using FSH.Modules.AssetRegister.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.AssetRegister.Integration;

/// <summary>
/// Reacts to <see cref="AssetReportedMissingFromCountEvent"/> raised when a count session is
/// reconciled with a Missing entry. Phase 4 logs the event for audit; the operator opens
/// the formal RLSDDSP incident report explicitly via <c>FileIncidentReportCommand</c>
/// (auto-draft creation from a SaveChanges callback would require a fresh service scope
/// to avoid nested-SaveChanges issues — deferred to a later phase if/when needed).
/// </summary>
public sealed class AssetReportedMissingFromCountHandler(
    ILogger<AssetReportedMissingFromCountHandler> logger)
    : INotificationHandler<AssetReportedMissingFromCountEvent>
{
    public ValueTask Handle(AssetReportedMissingFromCountEvent @event, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(@event);
        logger.LogWarning(
            "[{Tenant}] Asset {AssetRegistryId} reported MISSING during count session {SessionId}. " +
            "An RLSDDSP incident report should be filed via FileIncidentReportCommand.",
            @event.TenantId, @event.AssetRegistryId, @event.PhysicalCountSessionId);
        return ValueTask.CompletedTask;
    }
}
