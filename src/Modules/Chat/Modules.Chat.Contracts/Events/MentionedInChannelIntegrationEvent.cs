using AMIS.Framework.Eventing.Abstractions;

namespace AMIS.Modules.Chat.Contracts.Events;

/// <summary>
/// Raised once per distinct mentioned user when a message containing <c>@username</c> is sent.
/// Published so a future durable Notifications module can build a bell inbox without changing Chat.
/// The live SignalR <c>ChatMentioned</c> push is a separate, best-effort delivery.
/// </summary>
public sealed record MentionedInChannelIntegrationEvent(
    Guid ChannelId,
    Guid MessageId,
    string MentionedUserId,
    string SenderId,
    string? ContentPreview,
    string? TenantId,
    string CorrelationId) : IIntegrationEvent
{
    public Guid Id { get; } = Guid.NewGuid();

    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;

    public string Source => "Chat";
}
