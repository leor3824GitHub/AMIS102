using AMIS.Framework.Core.Context;
using AMIS.Modules.Chat.Contracts.v1;
using AMIS.Modules.Chat.Contracts.v1.Channels;
using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Modules.Chat.Data;
using AMIS.Modules.Chat.Domain;
using AMIS.Modules.Chat.Features.v1;
using Mediator;

namespace AMIS.Modules.Chat.Features.v1.Channels.CreateChannel;

public sealed class CreateChannelCommandHandler : ICommandHandler<CreateChannelCommand, ChannelDto>
{
    private readonly ChatDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreateChannelCommandHandler(ChatDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ChannelDto> Handle(CreateChannelCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId().ToString();
        var tenantId = _currentUser.GetTenant()
            ?? throw new InvalidOperationException("A tenant context is required to create a channel.");

        var channel = ChatChannel.CreateChannel(tenantId, command.Name, command.Topic, userId);
        channel.AddMember(userId, ChannelMemberRole.Admin);

        if (command.MemberUserIds is { Count: > 0 })
        {
            foreach (var memberId in command.MemberUserIds
                         .Where(id => !string.IsNullOrWhiteSpace(id) && !string.Equals(id, userId, StringComparison.OrdinalIgnoreCase))
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                channel.AddMember(memberId, ChannelMemberRole.Member);
            }
        }

        _dbContext.Channels.Add(channel);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return channel.ToDto();
    }
}
