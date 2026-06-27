using AMIS.Framework.Eventing.Abstractions;
using AMIS.Modules.Notifications.Contracts.v1.Enums;

namespace AMIS.Modules.Notifications.Contracts.Events;

/// <summary>
/// Published by any module that wants to drop a row into a user's notification inbox. The Notifications
/// module owns the only consumer, so producers stay fully decoupled from how the bell is stored/rendered.
/// Resolve <paramref name="RecipientUserId"/> to an identity user id (Guid string) at the producer.
/// </summary>
public sealed record NotificationRequestedIntegrationEvent(
    string RecipientUserId,
    NotificationType Type,
    string Title,
    string Body,
    string? Link,
    string Source,
    string? MetadataJson,
    string? TenantId,
    string CorrelationId) : IIntegrationEvent
{
    public Guid Id { get; } = Guid.NewGuid();

    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
