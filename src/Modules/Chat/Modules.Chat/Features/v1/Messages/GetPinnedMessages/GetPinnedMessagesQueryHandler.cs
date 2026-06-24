using AMIS.Framework.Core.Context;
using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Modules.Chat.Contracts.v1.Messages;
using AMIS.Modules.Chat.Data;
using AMIS.Modules.Chat.Features.v1;
using AMIS.Modules.Chat.Features.v1.Internal;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Chat.Features.v1.Messages.GetPinnedMessages;

/// <summary>Lists a channel's pinned messages, most recently pinned first. Tombstoned messages are excluded.</summary>
public sealed class GetPinnedMessagesQueryHandler : IQueryHandler<GetPinnedMessagesQuery, IReadOnlyList<MessageDto>>
{
    private readonly ChatDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetPinnedMessagesQueryHandler(ChatDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<IReadOnlyList<MessageDto>> Handle(GetPinnedMessagesQuery query, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId().ToString();
        await ChannelAuthorization
            .RequireMemberAsync(_dbContext, query.ChannelId, userId, cancellationToken)
            .ConfigureAwait(false);

        var pinned = await _dbContext.Messages
            .AsNoTracking()
            .Include(m => m.Mentions)
            .Include(m => m.Reactions)
            .Where(m => m.ChannelId == query.ChannelId && m.IsPinned && m.DeletedAtUtc == null)
            .OrderByDescending(m => m.PinnedOnUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return pinned.Select(m => m.ToDto()).ToList();
    }
}
