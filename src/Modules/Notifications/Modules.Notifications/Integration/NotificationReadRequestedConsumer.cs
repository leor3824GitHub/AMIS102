using AMIS.Framework.Eventing.Abstractions;
using AMIS.Modules.Notifications.Contracts.Events;
using AMIS.Modules.Notifications.Services;

namespace AMIS.Modules.Notifications.Integration;

/// <summary>
/// Workflow-driven mark-read: a module publishing a <see cref="NotificationReadRequestedIntegrationEvent"/>
/// (e.g. AssetRegister when an ICS/PAR is accepted) flips the matching inbox row to read via the shared
/// <see cref="INotificationWriter"/> — no user interaction with the bell required.
/// </summary>
internal sealed class NotificationReadRequestedConsumer(INotificationWriter writer)
    : IIntegrationEventHandler<NotificationReadRequestedIntegrationEvent>
{
    public Task HandleAsync(NotificationReadRequestedIntegrationEvent @event, CancellationToken ct = default)
        => writer.MarkReadAsync(@event, ct);
}
