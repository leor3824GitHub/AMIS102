using AMIS.Framework.Core.Context;
using AMIS.Modules.Chat.Contracts.v1;
using AMIS.Modules.Chat.Contracts.v1.Channels;
using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Modules.Chat.Data;
using AMIS.Modules.Chat.Features.v1;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Chat.Features.v1.Channels.ListMyChannels;

public sealed class ListMyChannelsQueryHandler : IQueryHandler<ListMyChannelsQuery, IReadOnlyList<ChannelDto>>
{
    private readonly ChatDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListMyChannelsQueryHandler(ChatDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<IReadOnlyList<ChannelDto>> Handle(ListMyChannelsQuery query, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId().ToString();

        var memberChannelIds = await _dbContext.ChannelMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.ChannelId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // The channel query filter keeps this office-or-global; the global channel is included via its scope
        // (implicit membership) without needing a membership row.
        var channels = await _dbContext.Channels
            .AsNoTracking()
            .Include(c => c.Members)
            .Where(c => memberChannelIds.Contains(c.Id) || c.Scope == ChannelScope.Global)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return channels
            .OrderByDescending(c => c.LastMessageAtUtc ?? c.CreatedOnUtc)
            .Select(c => c.ToDto())
            .ToList();
    }
}
