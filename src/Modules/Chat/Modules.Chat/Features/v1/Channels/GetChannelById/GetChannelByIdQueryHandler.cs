using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.Chat.Contracts.v1;
using AMIS.Modules.Chat.Contracts.v1.Channels;
using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Modules.Chat.Data;
using AMIS.Modules.Chat.Features.v1;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Chat.Features.v1.Channels.GetChannelById;

public sealed class GetChannelByIdQueryHandler : IQueryHandler<GetChannelByIdQuery, ChannelDto>
{
    private readonly ChatDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetChannelByIdQueryHandler(ChatDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ChannelDto> Handle(GetChannelByIdQuery query, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId().ToString();

        var channel = await _dbContext.Channels
            .AsNoTracking()
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == query.ChannelId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Channel not found.");

        // Non-members may not learn a channel exists (404, never 403). The global channel is implicit-membership.
        if (channel.Scope != ChannelScope.Global &&
            !channel.Members.Any(m => string.Equals(m.UserId, userId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new NotFoundException("Channel not found.");
        }

        return channel.ToDto();
    }
}
