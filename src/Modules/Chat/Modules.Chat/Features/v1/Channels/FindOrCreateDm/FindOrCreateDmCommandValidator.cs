using AMIS.Modules.Chat.Contracts.v1.Channels;
using FluentValidation;

namespace AMIS.Modules.Chat.Features.v1.Channels.FindOrCreateDm;

public sealed class FindOrCreateDmCommandValidator : AbstractValidator<FindOrCreateDmCommand>
{
    public FindOrCreateDmCommandValidator()
    {
        RuleFor(x => x.OtherUserId).NotEmpty().MaximumLength(64);
    }
}
