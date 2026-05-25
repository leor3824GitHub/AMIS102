using AMIS.Modules.AssetRegister.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.AssetRegister.Integration;

/// <summary>
/// Internal domain event handlers. These events track state changes within the module
/// but don't require integration events published to other modules. Handlers exist
/// to satisfy the Mediator library's requirement that all published notifications have
/// at least one handler.
/// </summary>

/// <summary>
/// Logs asset return from accountability. Internal tracking only — no integration event.
/// </summary>
public sealed class AssetReturnedEventHandler(ILogger<AssetReturnedEventHandler> logger)
    : INotificationHandler<AssetReturnedEvent>
{
    public ValueTask Handle(AssetReturnedEvent @event, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(@event);
        logger.LogInformation(
            "[{Tenant}] Asset {AssetRegistryId} returned from accountability {AccountabilityId}.",
            @event.TenantId, @event.AssetRegistryId, @event.AccountabilityId);
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Logs asset transfer between accountabilities. Internal tracking only — no integration event.
/// </summary>
public sealed class AssetTransferredEventHandler(ILogger<AssetTransferredEventHandler> logger)
    : INotificationHandler<AssetTransferredEvent>
{
    public ValueTask Handle(AssetTransferredEvent @event, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(@event);
        logger.LogInformation(
            "[{Tenant}] Asset {AssetRegistryId} transferred from accountability {FromAccountabilityId} to {ToAccountabilityId}.",
            @event.TenantId, @event.AssetRegistryId, @event.FromAccountabilityId, @event.ToAccountabilityId);
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Logs asset found at station during physical count. Internal tracking only — no integration event.
/// </summary>
public sealed class AssetFoundAtStationEventHandler(ILogger<AssetFoundAtStationEventHandler> logger)
    : INotificationHandler<AssetFoundAtStationEvent>
{
    public ValueTask Handle(AssetFoundAtStationEvent @event, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(@event);
        logger.LogInformation(
            "[{Tenant}] Asset found at station in count session {SessionId}, entry {EntryId}.",
            @event.TenantId, @event.PhysicalCountSessionId, @event.PhysicalCountEntryId);
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Logs asset marked as lost. Loss is formally recorded via IncidentReportFiled integration event,
/// so this handler just logs the internal state change.
/// </summary>
public sealed class AssetLostEventHandler(ILogger<AssetLostEventHandler> logger)
    : INotificationHandler<AssetLostEvent>
{
    public ValueTask Handle(AssetLostEvent @event, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(@event);
        logger.LogWarning(
            "[{Tenant}] Asset {AssetRegistryId} marked as LOST in incident report {IncidentReportId}.",
            @event.TenantId, @event.AssetRegistryId, @event.PropertyIncidentReportId);
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Logs asset recovered. Recovery is formally recorded via incident resolution,
/// so this handler just logs the internal state change.
/// </summary>
public sealed class AssetRecoveredEventHandler(ILogger<AssetRecoveredEventHandler> logger)
    : INotificationHandler<AssetRecoveredEvent>
{
    public ValueTask Handle(AssetRecoveredEvent @event, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(@event);
        logger.LogInformation(
            "[{Tenant}] Asset {AssetRegistryId} recovered in incident report {IncidentReportId}.",
            @event.TenantId, @event.AssetRegistryId, @event.PropertyIncidentReportId);
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Logs asset marked as unserviceable. Disposal is formally recorded via unserviceable report workflow,
/// so this handler just logs the internal state change.
/// </summary>
public sealed class AssetUnserviceableEventHandler(ILogger<AssetUnserviceableEventHandler> logger)
    : INotificationHandler<AssetUnserviceableEvent>
{
    public ValueTask Handle(AssetUnserviceableEvent @event, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(@event);
        logger.LogInformation(
            "[{Tenant}] Asset {AssetRegistryId} marked as UNSERVICEABLE in report {UnserviceableReportId}.",
            @event.TenantId, @event.AssetRegistryId, @event.UnserviceableReportId);
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Logs accountability cancellation. Internal tracking only — no integration event.
/// </summary>
public sealed class AccountabilityCancelledEventHandler(ILogger<AccountabilityCancelledEventHandler> logger)
    : INotificationHandler<AccountabilityCancelledEvent>
{
    public ValueTask Handle(AccountabilityCancelledEvent @event, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(@event);
        logger.LogInformation(
            "[{Tenant}] Accountability {AccountabilityId} cancelled. Reason: {Reason}.",
            @event.TenantId, @event.AccountabilityId, @event.Reason);
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Logs physical count session closure. Internal operation only — no integration event.
/// </summary>
public sealed class PhysicalCountSessionClosedEventHandler(ILogger<PhysicalCountSessionClosedEventHandler> logger)
    : INotificationHandler<PhysicalCountSessionClosedEvent>
{
    public ValueTask Handle(PhysicalCountSessionClosedEvent @event, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(@event);
        logger.LogInformation(
            "[{Tenant}] Physical count session {SessionId} closed.",
            @event.TenantId, @event.SessionId);
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Logs asset transferred out via issuance report. Internal transfer tracking only — no integration event.
/// The IssuanceReportPosted integration event already notifies other modules of the transfer.
/// </summary>
public sealed class AssetTransferredOutEventHandler(ILogger<AssetTransferredOutEventHandler> logger)
    : INotificationHandler<AssetTransferredOutEvent>
{
    public ValueTask Handle(AssetTransferredOutEvent @event, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(@event);
        logger.LogInformation(
            "[{Tenant}] Asset {AssetRegistryId} transferred out via issuance report {ReportNo} ({ReportType}).",
            @event.TenantId, @event.AssetRegistryId, @event.ReportNo, @event.ReportType);
        return ValueTask.CompletedTask;
    }
}
