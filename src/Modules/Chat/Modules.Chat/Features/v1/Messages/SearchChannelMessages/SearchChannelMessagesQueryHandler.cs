using AMIS.Framework.Core.Context;
using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Modules.Chat.Contracts.v1.Messages;
using AMIS.Modules.Chat.Data;
using AMIS.Modules.Chat.Features.v1;
using AMIS.Modules.Chat.Features.v1.Internal;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Chat.Features.v1.Messages.SearchChannelMessages;

/// <summary>
/// v1 message search: case-insensitive ILIKE substring match over <c>Message.Content</c> (no ranked FTS),
/// newest first, keyset-paginated like the channel message list. Tombstoned messages are excluded.
/// </summary>
public sealed class SearchChannelMessagesQueryHandler : IQueryHandler<SearchChannelMessagesQuery, MessagePageDto>
{
    private const int DefaultTake = 30;
    private const int MaxTake = 100;

    private readonly ChatDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public SearchChannelMessagesQueryHandler(ChatDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<MessagePageDto> Handle(SearchChannelMessagesQuery query, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId().ToString();
        await ChannelAuthorization
            .RequireMemberAsync(_dbContext, query.ChannelId, userId, cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(query.Query))
        {
            return new MessagePageDto([], null, false);
        }

        var take = Math.Clamp(query.Take ?? DefaultTake, 1, MaxTake);
        var pattern = $"%{query.Query.Trim()}%";

        var messages = _dbContext.Messages
            .AsNoTracking()
            .Include(m => m.Mentions)
            .Include(m => m.Reactions)
            .Where(m => m.ChannelId == query.ChannelId
                        && m.DeletedAtUtc == null
                        && EF.Functions.ILike(m.Content, pattern));

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
