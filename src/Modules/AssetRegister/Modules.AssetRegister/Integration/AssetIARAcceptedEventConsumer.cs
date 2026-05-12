using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.AssetProcurement.Contracts.v1.AssetInspectionAcceptanceReports;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.AssetRegister.Integration;

/// <summary>
/// Phase 1 stub for the AssetIARAccepted event consumer. The real materialization
/// path — expanding each accepted line into one AssetRegistry row per physical unit —
/// lands in Phase 3 alongside RegisterAssetCommand.
/// </summary>
internal sealed class AssetIARAcceptedEventConsumer(
    ILogger<AssetIARAcceptedEventConsumer> logger) : IIntegrationEventHandler<AssetIARAcceptedEvent>
{
    public Task HandleAsync(AssetIARAcceptedEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        logger.LogInformation(
            "[{Tenant}] AssetRegister received AssetIARAcceptedEvent IAR={IARId} PO={PoNumber} Items={ItemCount}. " +
            "Phase 1 stub: ignoring; materialization lands in Phase 3.",
            @event.TenantId, @event.IARId, @event.PoNumber, @event.AcceptedItems.Count);
        return Task.CompletedTask;
    }
}
