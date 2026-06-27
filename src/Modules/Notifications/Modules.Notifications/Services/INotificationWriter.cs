using AMIS.Modules.Notifications.Contracts.Events;

namespace AMIS.Modules.Notifications.Services;

/// <summary>
/// The single path that turns a <see cref="NotificationRequestedIntegrationEvent"/> into a durable inbox
/// row plus a best-effort live push. Reused by every consumer so storage + push behaviour stays in one place.
/// </summary>
internal interface INotificationWriter
{
    Task WriteAsync(NotificationRequestedIntegrationEvent request, CancellationToken cancellationToken);
}
