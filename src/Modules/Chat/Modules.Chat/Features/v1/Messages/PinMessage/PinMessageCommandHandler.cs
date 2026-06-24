using AMIS.Framework.Core.Context;
using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Modules.Chat.Contracts.v1.Messages;
using AMIS.Modules.Chat.Data;
using AMIS.Modules.Chat.Features.v1;
using AMIS.Modules.Chat.Features.v1.Internal;
using Mediator;

namespace AMIS.Modules.Chat.Features.v1.Messages.PinMessage;

public sealed class PinMessageCommandHandler : ICommandHandler<PinMessageCommand, MessageDto>
{
    private readonly ChatDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public PinMessageCommandHandler(ChatDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<MessageDto> Handle(PinMessageCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId().ToString();
        var message = await MessageAuthorization
            .RequireLiveMemberMessageAsync(_dbContext, command.MessageId, userId, cancellationToken)
            .ConfigureAwait(false);

        message.Pin(userId);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return message.ToDto();
    }
}
