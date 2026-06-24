using AMIS.Modules.Chat.Contracts.v1.DTOs;
using Mediator;

namespace AMIS.Modules.Chat.Contracts.v1.Messages;

/// <summary>
/// Send a message to a channel. <paramref name="ParentMessageId"/> starts/continues a single-level
/// thread (replies cannot themselves be replied to). <c>@username</c> tokens in
/// <paramref name="Content"/> are parsed and resolved server-side.
/// </summary>
public sealed record SendMessageCommand(
    Guid ChannelId,
    string Content,
    Guid? ParentMessageId = null) : ICommand<MessageDto>;

/// <summary>
/// List a channel's top-level messages using keyset (cursor) pagination, newest first.
/// <paramref name="Before"/> is the exclusive upper-bound message id (omit for the latest page);
/// keying on the time-sortable <c>Guid.CreateVersion7()</c> id keeps the cursor stable under a live feed.
/// </summary>
public sealed record ListChannelMessagesQuery(
    Guid ChannelId,
    Guid? Before = null,
    int? Take = null) : IQuery<MessagePageDto>;

/// <summary>Edit a message's content. Caller must be the original sender.</summary>
public sealed record EditMessageCommand(Guid MessageId, string Content) : ICommand<MessageDto>;

/// <summary>
/// Soft-delete (tombstone) a message. Caller must be the original sender. Returns the tombstoned
/// message so clients render a "message deleted" placeholder in place.
/// </summary>
public sealed record DeleteMessageCommand(Guid MessageId) : ICommand<MessageDto>;

/// <summary>Pin a message in its channel. Caller must be a channel member.</summary>
public sealed record PinMessageCommand(Guid MessageId) : ICommand<MessageDto>;

/// <summary>Unpin a message. Caller must be a channel member.</summary>
public sealed record UnpinMessageCommand(Guid MessageId) : ICommand<MessageDto>;

/// <summary>List the replies of a single-level thread (oldest first). Caller must be a channel member.</summary>
public sealed record ListMessageRepliesQuery(Guid MessageId) : IQuery<IReadOnlyList<MessageDto>>;

/// <summary>List a channel's pinned messages (most recently pinned first). Caller must be a channel member.</summary>
public sealed record GetPinnedMessagesQuery(Guid ChannelId) : IQuery<IReadOnlyList<MessageDto>>;

/// <summary>Add the caller's emoji reaction to a message. Idempotent. Caller must be a channel member.</summary>
public sealed record AddReactionCommand(Guid MessageId, string Emoji) : ICommand<MessageDto>;

/// <summary>Remove the caller's emoji reaction from a message. Caller must be a channel member.</summary>
public sealed record RemoveReactionCommand(Guid MessageId, string Emoji) : ICommand<MessageDto>;

/// <summary>
/// Search a channel's messages by content using a case-insensitive ILIKE substring match (v1 — no ranked FTS),
/// newest first, keyset-paginated. Tombstoned messages are excluded. Caller must be a channel member.
/// </summary>
public sealed record SearchChannelMessagesQuery(
    Guid ChannelId,
    string Query,
    Guid? Before = null,
    int? Take = null) : IQuery<MessagePageDto>;
