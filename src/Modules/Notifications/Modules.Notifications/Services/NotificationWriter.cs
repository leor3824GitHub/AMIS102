using AMIS.Framework.Web.Realtime;
using AMIS.Modules.Notifications.Contracts.Events;
using AMIS.Modules.Notifications.Data;
using AMIS.Modules.Notifications.Domain;
using AMIS.Modules.Notifications.Features.v1;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.Notifications.Services;

/// <summary>
/// Persists the inbox row, then (after commit) best-effort pushes <c>NotificationCreated</c> to the
/// recipient's <c>user:{id}</c> group. The push must never fail the write — a missing live update is
/// recovered on the next inbox fetch.
/// </summary>
internal sealed class NotificationWriter(
    NotificationsDbContext dbContext,
    IHubContext<AppHub> hub,
    ILogger<NotificationWriter> logger) : INotificationWriter
{
    public async Task WriteAsync(NotificationRequestedIntegrationEvent request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!Guid.TryParse(request.RecipientUserId, out var recipientGuid))
        {
            logger.LogWarning(
                "Notification from {Source} skipped: recipient '{Recipient}' is not a valid user id.",
                request.Source, request.RecipientUserId);
            return;
        }

        // Idempotency: the unique (CorrelationId, RecipientUserId, Type) index is the hard guarantee; this
        // pre-check keeps the happy path off the exception route on ordinary redelivery.
        var alreadyExists = await dbContext.Notifications
            .AnyAsync(
                n => n.CorrelationId == request.CorrelationId
                     && n.RecipientUserId == request.RecipientUserId
                     && n.Type == request.Type,
                cancellationToken)
            .ConfigureAwait(false);

        if (alreadyExists)
        {
            return;
        }

        var tenantId = request.TenantId ?? dbContext.TenantInfo?.Identifier ?? string.Empty;

        var notification = Notification.Create(
            tenantId,
            request.RecipientUserId,
            request.Type,
            request.Title,
            request.Body,
            request.Link,
            request.Source,
            request.MetadataJson,
            request.CorrelationId);

        dbContext.Notifications.Add(notification);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex)
        {
            // Lost the idempotency race to a concurrent redelivery — the row exists, which is the desired
            // outcome. Swallow rather than poison the message.
            logger.LogInformation(
                ex,
                "Notification ({Correlation}/{Type}) for {Recipient} already created by a concurrent delivery.",
                request.CorrelationId, request.Type, request.RecipientUserId);
            return;
        }

        // ---- After commit: live push is best-effort and must never fail the request. ----
        try
        {
            await hub.Clients.Group(AppHub.UserGroup(recipientGuid))
                .SendAsync(NotificationsModuleConstants.NotificationCreatedEvent, notification.ToDto(), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to push NotificationCreated for notification {Id}.", notification.Id);
        }
    }

    public async Task MarkReadAsync(NotificationReadRequestedIntegrationEvent request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!Guid.TryParse(request.RecipientUserId, out var recipientGuid))
        {
            logger.LogWarning(
                "Notification mark-read skipped: recipient '{Recipient}' is not a valid user id.",
                request.RecipientUserId);
            return;
        }

        var notification = await dbContext.Notifications
            .FirstOrDefaultAsync(
                n => n.CorrelationId == request.CorrelationId
                     && n.RecipientUserId == request.RecipientUserId
                     && n.Type == request.Type,
                cancellationToken)
            .ConfigureAwait(false);

        // Dismissed, never created (recipient had no login at issue time), or already read — nothing to do.
        if (notification is null || notification.IsRead)
        {
            return;
        }

        notification.MarkRead();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // ---- After commit: live push is best-effort and must never fail the request. ----
        try
        {
            await hub.Clients.Group(AppHub.UserGroup(recipientGuid))
                .SendAsync(NotificationsModuleConstants.NotificationReadEvent, notification.Id, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to push NotificationRead for notification {Id}.", notification.Id);
        }
    }
}
