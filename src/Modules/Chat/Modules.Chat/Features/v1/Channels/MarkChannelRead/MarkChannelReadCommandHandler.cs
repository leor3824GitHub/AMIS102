using AMIS.Framework.Core.Context;
using AMIS.Modules.Chat.Contracts.v1;
using AMIS.Modules.Chat.Contracts.v1.Channels;
using AMIS.Modules.Chat.Data;
using AMIS.Modules.Chat.Features.v1.Internal;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Chat.Features.v1.Channels.MarkChannelRead;

public sealed class MarkChannelReadCommandHandler : ICommandHandler<MarkChannelReadCommand, Unit>
{
    private readonly ChatDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public MarkChannelReadCommandHandler(ChatDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<Unit> Handle(MarkChannelReadCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId().ToString();
        var channel = await ChannelAuthorization
            .RequireMemberAsync(_dbContext, command.ChannelId, userId, cancellationToken)
            .ConfigureAwait(false);

        // The global channel has implicit membership (no member rows), so read tracking is a no-op in v1.
        if (channel.Scope == ChannelScope.Global)
        {
            return Unit.Value;
        }

        var member = await _dbContext.ChannelMembers
            .FirstOrDefaultAsync(m => m.ChannelId == command.ChannelId && m.UserId == userId, cancellationToken)
            .ConfigureAwait(false);

        if (member is not null)
        {
            member.MarkRead(command.MessageId);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Unit.Value;
    }
}
