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
