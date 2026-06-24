using AMIS.Modules.Chat.Contracts.v1.Channels;
using FluentValidation;

namespace AMIS.Modules.Chat.Features.v1.Channels.RestoreChannel;

public sealed class RestoreChannelCommandValidator : AbstractValidator<RestoreChannelCommand>
{
    public RestoreChannelCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
    }
}
