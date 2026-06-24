using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.Chat.Domain;

/// <summary>A resolved <c>@user</c> mention on a message.</summary>
public sealed class MessageMention : BaseEntity<Guid>
{
    private MessageMention() { } // EF

    public Guid MessageId { get; private set; }

    public string MentionedUserId { get; private set; } = default!;

    public DateTimeOffset CreatedOnUtc { get; private set; }

    internal static MessageMention Create(Guid messageId, string mentionedUserId) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            MessageId = messageId,
            MentionedUserId = mentionedUserId,
            CreatedOnUtc = DateTimeOffset.UtcNow,
        };
}
