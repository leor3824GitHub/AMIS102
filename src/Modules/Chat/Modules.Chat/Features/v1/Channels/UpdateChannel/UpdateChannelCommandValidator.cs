using AMIS.Modules.Chat.Contracts.v1.Channels;
using FluentValidation;

namespace AMIS.Modules.Chat.Features.v1.Channels.UpdateChannel;

public sealed class UpdateChannelCommandValidator : AbstractValidator<UpdateChannelCommand>
{
    public UpdateChannelCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Topic).MaximumLength(1000);
    }
}
