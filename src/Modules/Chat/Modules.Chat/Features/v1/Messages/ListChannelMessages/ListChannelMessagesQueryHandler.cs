using AMIS.Framework.Core.Context;
using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Modules.Chat.Contracts.v1.Messages;
using AMIS.Modules.Chat.Data;
using AMIS.Modules.Chat.Features.v1;
using AMIS.Modules.Chat.Features.v1.Internal;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Chat.Features.v1.Messages.ListChannelMessages;

/// <summary>
/// Keyset (cursor) pagination, newest first. The cursor is the time-sortable <c>Guid.CreateVersion7()</c>
/// message id; it is resolved to the message's <see cref="Domain.Message.CreatedOnUtc"/> to drive the keyset
/// (Guid has no SQL ordering operator, but v7 ids are monotonic with creation time, so this is equivalent).
/// Tombstoned messages are included so the UI can render a "deleted" placeholder without shifting the window.
/// </summary>
public sealed class ListChannelMessagesQueryHandler : IQueryHandler<ListChannelMessagesQuery, MessagePageDto>
{
    private const int DefaultTake = 30;
    private const int MaxTake = 100;

    private readonly ChatDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListChannelMessagesQueryHandler(ChatDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<MessagePageDto> Handle(ListChannelMessagesQuery query, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId().ToString();
        await ChannelAuthorization.RequireMemberAsync(_dbContext, query.ChannelId, userId, cancellationToken).ConfigureAwait(false);

        var take = Math.Clamp(query.Take ?? DefaultTake, 1, MaxTake);

        var messages = _dbContext.Messages
            .AsNoTracking()
            .Include(m => m.Mentions)
            .Include(m => m.Reactions)
            .Where(m => m.ChannelId == query.ChannelId && m.ParentMessageId == null);

        if (query.Before is { } beforeId)
        {
            var beforeCreatedOnUtc = await _dbContext.Messages
                .Where(m => m.Id == beforeId)
                .Select(m => (DateTimeOffset?)m.CreatedOnUtc)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (beforeCreatedOnUtc is { } cursor)
            {
                messages = messages.Where(m => m.CreatedOnUtc < cursor);
            }
        }

        // Fetch one extra row to determine whether an older page exists.
        var page = await messages
            .OrderByDescending(m => m.CreatedOnUtc)
            .Take(take + 1)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var hasMore = page.Count > take;
        var items = page.Take(take).Select(m => m.ToDto()).ToList();
        var nextCursor = hasMore && items.Count > 0 ? items[^1].Id : (Guid?)null;

        return new MessagePageDto(items, nextCursor, hasMore);
    }
}
