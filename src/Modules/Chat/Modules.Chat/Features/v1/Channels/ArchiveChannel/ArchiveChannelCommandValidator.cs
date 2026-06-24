using AMIS.Modules.Chat.Contracts.v1.Channels;
using FluentValidation;

namespace AMIS.Modules.Chat.Features.v1.Channels.ArchiveChannel;

public sealed class ArchiveChannelCommandValidator : AbstractValidator<ArchiveChannelCommand>
{
    public ArchiveChannelCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
    }
}
