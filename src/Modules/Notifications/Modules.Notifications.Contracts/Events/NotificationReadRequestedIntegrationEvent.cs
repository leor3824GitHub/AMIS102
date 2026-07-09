using AMIS.Framework.Eventing.Abstractions;
using AMIS.Modules.Notifications.Contracts.v1.Enums;

namespace AMIS.Modules.Notifications.Contracts.Events;

/// <summary>
/// Published by a producing module when the workflow action a notification asked for has been completed
/// (e.g. an ICS/PAR was accepted), so the inbox row flips to read without the recipient touching the bell.
/// Targets the same (RecipientUserId, Type, CorrelationId) key the original
/// <see cref="NotificationRequestedIntegrationEvent"/> was written with; a missing row (dismissed, never
/// created) is a silent no-op.
/// </summary>
public sealed record NotificationReadRequestedIntegrationEvent(
    string RecipientUserId,
    NotificationType Type,
    string Source,
    string CorrelationId,
    string? TenantId) : IIntegrationEvent
{
    public Guid Id { get; } = Guid.NewGuid();

    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
