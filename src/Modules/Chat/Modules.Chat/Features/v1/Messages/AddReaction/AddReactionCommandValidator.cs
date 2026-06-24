using AMIS.Modules.Chat.Contracts.v1.Messages;
using FluentValidation;

namespace AMIS.Modules.Chat.Features.v1.Messages.AddReaction;

public sealed class AddReactionCommandValidator : AbstractValidator<AddReactionCommand>
{
    public AddReactionCommandValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.Emoji).NotEmpty().MaximumLength(32);
    }
}
