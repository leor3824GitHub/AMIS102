using AMIS.Framework.Core.Context;
using AMIS.Modules.Chat.Contracts.v1;
using AMIS.Modules.Chat.Contracts.v1.Channels;
using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Modules.Chat.Data;
using AMIS.Modules.Chat.Features.v1;
using AMIS.Modules.Chat.Features.v1.Internal;
using FluentValidation;
using FluentValidation.Results;
using Mediator;

namespace AMIS.Modules.Chat.Features.v1.Channels.UpdateChannel;

public sealed class UpdateChannelCommandHandler : ICommandHandler<UpdateChannelCommand, ChannelDto>
{
    private readonly ChatDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public UpdateChannelCommandHandler(ChatDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ChannelDto> Handle(UpdateChannelCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId().ToString();
        var channel = await ChannelAuthorization
            .RequireAdminMemberAsync(_dbContext, command.ChannelId, userId, cancellationToken)
            .ConfigureAwait(false);

        if (channel.Type == ChannelType.Direct)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(command.ChannelId), "Direct messages cannot be renamed.")
            ]);
        }

        channel.Rename(command.Name, command.Topic);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return channel.ToDto();
    }
}
