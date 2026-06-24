using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.Chat.Domain;

/// <summary>An emoji reaction by a user on a message. Unique per (message, user, emoji).</summary>
public sealed class MessageReaction : BaseEntity<Guid>
{
    private MessageReaction() { } // EF

    public Guid MessageId { get; private set; }

    public string UserId { get; private set; } = default!;

    public string Emoji { get; private set; } = default!;

    public DateTimeOffset CreatedOnUtc { get; private set; }

    internal static MessageReaction Create(Guid messageId, string userId, string emoji) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            MessageId = messageId,
            UserId = userId,
            Emoji = emoji,
            CreatedOnUtc = DateTimeOffset.UtcNow,
        };
}
