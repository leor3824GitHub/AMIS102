using AMIS.Framework.Eventing.Abstractions;
using AMIS.Modules.Chat.Contracts.Events;
using AMIS.Modules.Notifications.Contracts.Events;
using AMIS.Modules.Notifications.Contracts.v1.Enums;
using AMIS.Modules.Notifications.Services;

namespace AMIS.Modules.Notifications.Integration;

/// <summary>
/// Adapts the Chat-shaped <see cref="MentionedInChannelIntegrationEvent"/> (already published by Chat) into
/// a notification, lighting up the bell for @mentions without any change to the Chat module.
/// </summary>
internal sealed class MentionedInChannelConsumer(INotificationWriter writer)
    : IIntegrationEventHandler<MentionedInChannelIntegrationEvent>
{
    public Task HandleAsync(MentionedInChannelIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var request = new NotificationRequestedIntegrationEvent(
            RecipientUserId: @event.MentionedUserId,
            Type: NotificationType.ChatMention,
            Title: "You were mentioned",
            Body: string.IsNullOrWhiteSpace(@event.ContentPreview)
                ? "You were mentioned in a channel."
                : @event.ContentPreview!,
            Link: $"/chat?channel={@event.ChannelId}&message={@event.MessageId}",
            Source: "Chat",
            MetadataJson: null,
            TenantId: @event.TenantId,
            CorrelationId: @event.CorrelationId);

        return writer.WriteAsync(request, ct);
    }
}
