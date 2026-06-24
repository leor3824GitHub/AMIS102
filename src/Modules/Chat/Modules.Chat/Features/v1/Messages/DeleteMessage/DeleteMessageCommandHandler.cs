using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Modules.Chat.Contracts.v1.Messages;
using AMIS.Modules.Chat.Data;
using AMIS.Modules.Chat.Features.v1;
using AMIS.Modules.Chat.Features.v1.Internal;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Chat.Features.v1.Messages.DeleteMessage;

public sealed class DeleteMessageCommandHandler : ICommandHandler<DeleteMessageCommand, MessageDto>
{
    private readonly ChatDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public DeleteMessageCommandHandler(ChatDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<MessageDto> Handle(DeleteMessageCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId().ToString();

        var message = await _dbContext.Messages
            .Include(m => m.Mentions)
            .Include(m => m.Reactions)
            .FirstOrDefaultAsync(m => m.Id == command.MessageId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Message not found.");

        await ChannelAuthorization
            .RequireMemberAsync(_dbContext, message.ChannelId, userId, cancellationToken)
            .ConfigureAwait(false);

        // Already tombstoned — return the placeholder dto without re-checking ownership (idempotent).
        if (message.IsDeleted)
        {
            return message.ToDto();
        }

        if (!string.Equals(message.SenderId, userId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException("You can only delete your own messages.");
        }

        message.SoftDelete(userId);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return message.ToDto();
    }
}
