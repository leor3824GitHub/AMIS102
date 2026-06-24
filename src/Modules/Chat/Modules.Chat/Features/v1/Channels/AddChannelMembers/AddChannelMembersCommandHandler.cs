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

namespace AMIS.Modules.Chat.Features.v1.Channels.AddChannelMembers;

public sealed class AddChannelMembersCommandHandler : ICommandHandler<AddChannelMembersCommand, ChannelDto>
{
    private readonly ChatDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public AddChannelMembersCommandHandler(ChatDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ChannelDto> Handle(AddChannelMembersCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId().ToString();
        var channel = await ChannelAuthorization
            .RequireAdminMemberAsync(_dbContext, command.ChannelId, userId, cancellationToken)
            .ConfigureAwait(false);

        if (channel.Type == ChannelType.Direct)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(command.UserIds), "Members cannot be added to a direct message.")
            ]);
        }

        foreach (var memberId in command.UserIds
                     .Where(id => !string.IsNullOrWhiteSpace(id))
                     .Select(id => id.Trim())
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            channel.AddMember(memberId, ChannelMemberRole.Member); // idempotent — existing members are skipped
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return channel.ToDto();
    }
}
