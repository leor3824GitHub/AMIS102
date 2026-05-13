using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.AssetRegister.Contracts.v1.IntegrationEvents;
using FSH.Modules.AssetRegister.Data;
using FSH.Modules.AssetRegister.Domain.Events;
using Mediator;

namespace FSH.Modules.AssetRegister.Integration;

/// <summary>
/// Subscribes to in-process domain events (raised by aggregates and dispatched on
/// SaveChanges) and republishes the cross-module ones to the integration event bus.
/// </summary>
public sealed class AssetRegisteredIntegrationPublisher(
    IEventBus bus, AssetRegisterDbContext db) : INotificationHandler<AssetRegisteredEvent>
{
    public async ValueTask Handle(AssetRegisteredEvent @event, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(@event);
        var asset = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
            db.AssetRegistries, a => a.Id == @event.AssetRegistryId, ct).ConfigureAwait(false);
        if (asset is null) return;

        await bus.PublishAsync(new AssetRegisterIntegrationEvents.AssetRegistered(
            asset.Id, asset.PropertyNo.Value, asset.AssetType, asset.UnitCost,
            asset.AcquisitionDate, @event.TenantId, @event.CorrelationId ?? string.Empty), ct).ConfigureAwait(false);
    }
}

public sealed class AssetIssuedIntegrationPublisher(IEventBus bus) : INotificationHandler<AssetIssuedEvent>
{
    public ValueTask Handle(AssetIssuedEvent @event, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(@event);
        return new ValueTask(bus.PublishAsync(new AssetRegisterIntegrationEvents.AssetIssued(
            @event.AssetRegistryId, @event.AccountabilityId, @event.CustodianId,
            @event.TenantId, @event.CorrelationId ?? string.Empty), ct));
    }
}

public sealed class AssetDisposedIntegrationPublisher(IEventBus bus) : INotificationHandler<AssetDisposedEvent>
{
    public ValueTask Handle(AssetDisposedEvent @event, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(@event);
        return new ValueTask(bus.PublishAsync(new AssetRegisterIntegrationEvents.AssetDisposed(
            @event.AssetRegistryId, @event.PropertyNo, @event.TenantId,
            @event.CorrelationId ?? string.Empty), ct));
    }
}

public sealed class IssuanceReportPostedIntegrationPublisher(
    IEventBus bus, AssetRegisterDbContext db) : INotificationHandler<IssuanceReportPostedEvent>
{
    public async ValueTask Handle(IssuanceReportPostedEvent @event, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(@event);
        var report = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
            db.PropertyIssuanceReports, r => r.Id == @event.ReportId, ct).ConfigureAwait(false);
        if (report is null) return;

        await bus.PublishAsync(new AssetRegisterIntegrationEvents.IssuanceReportPosted(
            report.Id, report.ReportNo, report.ReportType, report.PeriodFromDate,
            report.PeriodToDate, @event.TenantId, @event.CorrelationId ?? string.Empty), ct).ConfigureAwait(false);
    }
}

public sealed class IncidentReportFiledIntegrationPublisher(
    IEventBus bus, AssetRegisterDbContext db) : INotificationHandler<IncidentReportFiledEvent>
{
    public async ValueTask Handle(IncidentReportFiledEvent @event, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(@event);
        var report = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
            db.PropertyIncidentReports, r => r.Id == @event.IncidentReportId, ct).ConfigureAwait(false);
        if (report is null) return;

        await bus.PublishAsync(new AssetRegisterIntegrationEvents.IncidentReportFiled(
            report.Id, report.IncidentNo, report.IncidentType,
            @event.TenantId, @event.CorrelationId ?? string.Empty), ct).ConfigureAwait(false);
    }
}
