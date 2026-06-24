using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Framework.Eventing.Abstractions;
using AMIS.Framework.Web.Realtime;
using AMIS.Modules.Chat.Contracts.Events;
using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Modules.Chat.Contracts.v1.Messages;
using AMIS.Modules.Chat.Data;
using AMIS.Modules.Chat.Domain;
using AMIS.Modules.Chat.Features.v1;
using AMIS.Modules.Chat.Features.v1.Internal;
using AMIS.Modules.Chat.Services;
using FluentValidation;
using FluentValidation.Results;
using Mediator;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.Chat.Features.v1.Messages.SendMessage;

/// <summary>
/// The spine of the feature: membership → validate parent (single-level threads) → parse/resolve mentions
/// (drop self + unresolved) → persist message + mentions → bump reply count + touch channel → COMMIT →
/// only then broadcast <c>ChatMessageCreated</c> and best-effort push <c>ChatMentioned</c> + publish the
/// mention integration event for a future durable Notifications consumer.
/// </summary>
public sealed class SendMessageCommandHandler : ICommandHandler<SendMessageCommand, MessageDto>
{
    private readonly ChatDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IMentionResolver _mentionResolver;
    private readonly IHubContext<AppHub> _hub;
    private readonly IEventBus _eventBus;
    private readonly ILogger<SendMessageCommandHandler> _logger;

    public SendMessageCommandHandler(
        ChatDbContext dbContext,
        ICurrentUser currentUser,
        IMentionResolver mentionResolver,
        IHubContext<AppHub> hub,
        IEventBus eventBus,
        ILogger<SendMessageCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _mentionResolver = mentionResolver;
        _hub = hub;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async ValueTask<MessageDto> Handle(SendMessageCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId().ToString();
        var tenantId = _currentUser.GetTenant();

        var channel = await ChannelAuthorization
            .RequireMemberAsync(_dbContext, command.ChannelId, userId, cancellationToken)
            .ConfigureAwait(false);

        // Single-level threads only: a reply may target a top-level message, never another reply.
        Message? parent = null;
        if (command.ParentMessageId is { } parentId)
        {
            parent = await _dbContext.Messages
                .FirstOrDefaultAsync(m => m.Id == parentId && m.ChannelId == command.ChannelId, cancellationToken)
                .ConfigureAwait(false);

            if (parent is null || parent.IsDeleted)
            {
                throw new NotFoundException("Parent message not found.");
            }

            if (parent.ParentMessageId is not null)
            {
                throw new ValidationException(
                [
                    new ValidationFailure(nameof(command.ParentMessageId), "Replies cannot be nested (single-level threads only).")
                ]);
            }
        }

        // Parse @username tokens, resolve to active users, drop self and unresolved.
        var usernames = MentionParser.Parse(command.Content);
        var resolved = await _mentionResolver.ResolveAsync(usernames, cancellationToken).ConfigureAwait(false);
        var mentioned = resolved
            .Where(r => !string.Equals(r.UserId, userId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var senderName = await _mentionResolver.ResolveDisplayNameAsync(userId, cancellationToken).ConfigureAwait(false);

        var message = Message.Create(command.ChannelId, userId, senderName, command.Content.Trim(), command.ParentMessageId, tenantId);
        foreach (var mention in mentioned)
        {
            message.AddMention(mention.UserId);
        }

        _dbContext.Messages.Add(message);
        channel.TouchLastMessage(message.Id, message.CreatedOnUtc);
        parent?.IncrementReplyCount();

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var dto = message.ToDto();

        // ---- After commit: broadcast is best-effort and must never fail the request. ----
        await BroadcastAsync(command.ChannelId, dto, userId, message.Id, mentioned, tenantId, command.Content, cancellationToken)
            .ConfigureAwait(false);

        return dto;
    }

    private async Task BroadcastAsync(
        Guid channelId,
        MessageDto dto,
        string senderId,
        Guid messageId,
        List<ResolvedMention> mentioned,
        string? tenantId,
        string content,
        CancellationToken cancellationToken)
    {
        try
        {
            await _hub.Clients.Group(AppHub.ChannelGroup(channelId))
                .SendAsync(RealtimeEvents.ChatMessageCreated, dto, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast ChatMessageCreated for message {MessageId}.", messageId);
        }

        if (mentioned.Count == 0)
        {
            return;
        }

        var preview = content.Length > 140 ? content[..140] : content;

        foreach (var mention in mentioned)
        {
            if (!Guid.TryParse(mention.UserId, out var mentionedGuid))
            {
                continue;
            }

            try
            {
                await _hub.Clients.Group(AppHub.UserGroup(mentionedGuid))
                    .SendAsync(RealtimeEvents.ChatMentioned,
                        new { ChannelId = channelId, MessageId = messageId, SenderId = senderId },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to push ChatMentioned to user {UserId}.", mention.UserId);
            }
        }

        // Published for a future durable Notifications module; the live push above is the v1 delivery.
        var correlationId = messageId.ToString();
        var events = mentioned
            .Select(m => new MentionedInChannelIntegrationEvent(channelId, messageId, m.UserId, senderId, preview, tenantId, correlationId))
            .ToList<IIntegrationEvent>();

        try
        {
            await _eventBus.PublishAsync(events, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish mention integration events for message {MessageId}.", messageId);
        }
    }
}
