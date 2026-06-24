using AMIS.Framework.Core.Context;
using AMIS.Modules.Chat.Contracts.v1.Channels;
using AMIS.Modules.Chat.Data;
using AMIS.Modules.Chat.Features.v1.Internal;
using Mediator;

namespace AMIS.Modules.Chat.Features.v1.Channels.RemoveChannelMember;

public sealed class RemoveChannelMemberCommandHandler : ICommandHandler<RemoveChannelMemberCommand, Unit>
{
    private readonly ChatDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public RemoveChannelMemberCommandHandler(ChatDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<Unit> Handle(RemoveChannelMemberCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId().ToString();
        var channel = await ChannelAuthorization
            .RequireAdminMemberAsync(_dbContext, command.ChannelId, userId, cancellationToken)
            .ConfigureAwait(false);

        // Removing from the tracked aggregate collection deletes the orphan row (required FK + cascade).
        channel.RemoveMember(command.UserId.Trim());
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
