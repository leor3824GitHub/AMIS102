using AMIS.Modules.Chat.Contracts.v1.Channels;
using FluentValidation;

namespace AMIS.Modules.Chat.Features.v1.Channels.AddChannelMembers;

public sealed class AddChannelMembersCommandValidator : AbstractValidator<AddChannelMembersCommand>
{
    public AddChannelMembersCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.UserIds).NotEmpty();
        RuleForEach(x => x.UserIds).NotEmpty().MaximumLength(64);
    }
}
