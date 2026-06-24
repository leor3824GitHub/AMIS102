using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.Chat.Domain;

/// <summary>
/// A chat message. Soft delete is a <see cref="DeletedAtUtc"/> tombstone (NOT <see cref="ISoftDeletable"/>),
/// so it has no global query filter — every read slice must hand-filter <c>DeletedAtUtc IS NULL</c> (or render
/// a "deleted" placeholder). Mentions and reactions are child entities owned by the message.
/// </summary>
public sealed class Message : BaseEntity<Guid>
{
    private readonly List<MessageMention> _mentions = [];
    private readonly List<MessageReaction> _reactions = [];

    private Message() { } // EF

    public Guid ChannelId { get; private set; }

    public string SenderId { get; private set; } = default!;

    public string Content { get; private set; } = default!;

    public Guid? ParentMessageId { get; private set; }

    public int ReplyCount { get; private set; }

    public bool IsPinned { get; private set; }

    public DateTimeOffset? PinnedOnUtc { get; private set; }

    public string? PinnedBy { get; private set; }

    /// <summary>Denormalized owning office for display/reporting. Never used as a query filter.</summary>
    public string? TenantId { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public DateTimeOffset? EditedOnUtc { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public string? DeletedBy { get; private set; }

    public bool IsDeleted => DeletedAtUtc is not null;

    public IReadOnlyCollection<MessageMention> Mentions => _mentions;

    public IReadOnlyCollection<MessageReaction> Reactions => _reactions;

    public static Message Create(Guid channelId, string senderId, string content, Guid? parentMessageId, string? tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(senderId);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        return new Message
        {
            Id = Guid.CreateVersion7(),
            ChannelId = channelId,
            SenderId = senderId,
            Content = content,
            ParentMessageId = parentMessageId,
            TenantId = tenantId,
            CreatedOnUtc = DateTimeOffset.UtcNow,
        };
    }

    public void Edit(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        Content = content;
        EditedOnUtc = DateTimeOffset.UtcNow;
    }

    public void SoftDelete(string by)
    {
        if (IsDeleted)
        {
            return;
        }

        DeletedAtUtc = DateTimeOffset.UtcNow;
        DeletedBy = by;
    }

    public void Pin(string by)
    {
        IsPinned = true;
        PinnedOnUtc = DateTimeOffset.UtcNow;
        PinnedBy = by;
    }

    public void Unpin()
    {
        IsPinned = false;
        PinnedOnUtc = null;
        PinnedBy = null;
    }

    public void IncrementReplyCount() => ReplyCount++;

    public MessageMention AddMention(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var existing = _mentions.Find(m => string.Equals(m.MentionedUserId, userId, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return existing;
        }

        var mention = MessageMention.Create(Id, userId);
        _mentions.Add(mention);
        return mention;
    }

    /// <summary>Idempotent: a duplicate (user, emoji) is ignored (also enforced by a unique index).</summary>
    public void AddReaction(string userId, string emoji)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(emoji);

        var existing = _reactions.Find(r =>
            string.Equals(r.UserId, userId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(r.Emoji, emoji, StringComparison.Ordinal));
        if (existing is not null)
        {
            return;
        }

        _reactions.Add(MessageReaction.Create(Id, userId, emoji));
    }

    public void RemoveReaction(string userId, string emoji)
    {
        var existing = _reactions.Find(r =>
            string.Equals(r.UserId, userId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(r.Emoji, emoji, StringComparison.Ordinal));
        if (existing is not null)
        {
            _reactions.Remove(existing);
        }
    }
}
