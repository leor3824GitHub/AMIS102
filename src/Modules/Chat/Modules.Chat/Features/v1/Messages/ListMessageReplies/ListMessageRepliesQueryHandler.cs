using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Modules.Chat.Contracts.v1.Messages;
using AMIS.Modules.Chat.Data;
using AMIS.Modules.Chat.Features.v1;
using AMIS.Modules.Chat.Features.v1.Internal;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Chat.Features.v1.Messages.ListMessageReplies;

/// <summary>
/// Lists a single-level thread's replies, oldest first. Tombstoned replies are kept (rendered as a "deleted"
/// placeholder) so the thread does not visibly re-order, consistent with the channel message list.
/// </summary>
public sealed class ListMessageRepliesQueryHandler : IQueryHandler<ListMessageRepliesQuery, IReadOnlyList<MessageDto>>
{
    private readonly ChatDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListMessageRepliesQueryHandler(ChatDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<IReadOnlyList<MessageDto>> Handle(ListMessageRepliesQuery query, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId().ToString();

        var parentChannelId = await _dbContext.Messages
            .Where(m => m.Id == query.MessageId)
            .Select(m => (Guid?)m.ChannelId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Message not found.");

        await ChannelAuthorization
            .RequireMemberAsync(_dbContext, parentChannelId, userId, cancellationToken)
            .ConfigureAwait(false);

        var replies = await _dbContext.Messages
            .AsNoTracking()
            .Include(m => m.Mentions)
            .Include(m => m.Reactions)
            .Where(m => m.ParentMessageId == query.MessageId)
            .OrderBy(m => m.CreatedOnUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return replies.Select(m => m.ToDto()).ToList();
    }
}
