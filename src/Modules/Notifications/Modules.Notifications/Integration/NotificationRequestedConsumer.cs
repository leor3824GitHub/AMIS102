using AMIS.Framework.Eventing.Abstractions;
using AMIS.Modules.Notifications.Contracts.Events;
using AMIS.Modules.Notifications.Services;

namespace AMIS.Modules.Notifications.Integration;

/// <summary>
/// The generic entry point: any module publishing a <see cref="NotificationRequestedIntegrationEvent"/>
/// lands a row in the recipient's inbox via the shared <see cref="INotificationWriter"/>.
/// </summary>
internal sealed class NotificationRequestedConsumer(INotificationWriter writer)
    : IIntegrationEventHandler<NotificationRequestedIntegrationEvent>
{
    public Task HandleAsync(NotificationRequestedIntegrationEvent @event, CancellationToken ct = default)
        => writer.WriteAsync(@event, ct);
}
