using AMIS.Framework.Core.Context;
using AMIS.Modules.Chat.Contracts.v1.Channels;
using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Modules.Chat.Data;
using AMIS.Modules.Chat.Features.v1;
using AMIS.Modules.Chat.Features.v1.Internal;
using Mediator;

namespace AMIS.Modules.Chat.Features.v1.Channels.RestoreChannel;

public sealed class RestoreChannelCommandHandler : ICommandHandler<RestoreChannelCommand, ChannelDto>
{
    private readonly ChatDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public RestoreChannelCommandHandler(ChatDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ChannelDto> Handle(RestoreChannelCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId().ToString();

        // The channel is soft-deleted, so the office-or-global query filter hides it — load past the filter.
        var channel = await ChannelAuthorization
            .RequireAdminMemberAsync(_dbContext, command.ChannelId, userId, cancellationToken, includeArchived: true)
            .ConfigureAwait(false);

        channel.Restore();
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return channel.ToDto();
    }
}
