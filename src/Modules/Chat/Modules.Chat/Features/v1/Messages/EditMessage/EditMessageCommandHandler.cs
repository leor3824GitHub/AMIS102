using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Modules.Chat.Contracts.v1.Messages;
using AMIS.Modules.Chat.Data;
using AMIS.Modules.Chat.Features.v1;
using AMIS.Modules.Chat.Features.v1.Internal;
using Mediator;

namespace AMIS.Modules.Chat.Features.v1.Messages.EditMessage;

public sealed class EditMessageCommandHandler : ICommandHandler<EditMessageCommand, MessageDto>
{
    private readonly ChatDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public EditMessageCommandHandler(ChatDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<MessageDto> Handle(EditMessageCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId().ToString();
        var message = await MessageAuthorization
            .RequireLiveMemberMessageAsync(_dbContext, command.MessageId, userId, cancellationToken)
            .ConfigureAwait(false);

        if (!string.Equals(message.SenderId, userId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException("You can only edit your own messages.");
        }

        message.Edit(command.Content.Trim());
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return message.ToDto();
    }
}
