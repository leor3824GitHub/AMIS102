using AMIS.Modules.Notifications.Contracts.Events;

namespace AMIS.Modules.Notifications.Services;

/// <summary>
/// The single path that mutates inbox rows from integration events — create
/// (<see cref="NotificationRequestedIntegrationEvent"/>) and workflow-driven mark-read
/// (<see cref="NotificationReadRequestedIntegrationEvent"/>) — each with a best-effort live push.
/// Reused by every consumer so storage + push behaviour stays in one place.
/// </summary>
internal interface INotificationWriter
{
    Task WriteAsync(NotificationRequestedIntegrationEvent request, CancellationToken cancellationToken);

    Task MarkReadAsync(NotificationReadRequestedIntegrationEvent request, CancellationToken cancellationToken);
}
