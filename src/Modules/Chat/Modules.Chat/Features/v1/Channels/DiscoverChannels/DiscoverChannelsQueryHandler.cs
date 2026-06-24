using AMIS.Framework.Core.Context;
using AMIS.Modules.Chat.Contracts.v1;
using AMIS.Modules.Chat.Contracts.v1.Channels;
using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Modules.Chat.Data;
using AMIS.Modules.Chat.Features.v1;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Chat.Features.v1.Channels.DiscoverChannels;

/// <summary>
/// Lists office-scoped named channels in the caller's office that they have not yet joined. The channel query
/// filter keeps results office-or-global; restricting to <see cref="ChannelScope.Office"/> drops the global
/// channel (already returned by ListMyChannels) and only the caller's office channels remain.
/// </summary>
public sealed class DiscoverChannelsQueryHandler : IQueryHandler<DiscoverChannelsQuery, IReadOnlyList<ChannelDto>>
{
    private readonly ChatDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public DiscoverChannelsQueryHandler(ChatDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<IReadOnlyList<ChannelDto>> Handle(DiscoverChannelsQuery query, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId().ToString();

        var myChannelIds = await _dbContext.ChannelMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.ChannelId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var channels = await _dbContext.Channels
            .AsNoTracking()
            .Include(c => c.Members)
            .Where(c => c.Type == ChannelType.Named
                        && c.Scope == ChannelScope.Office
                        && !myChannelIds.Contains(c.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return channels
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Select(c => c.ToDto())
            .ToList();
    }
}
