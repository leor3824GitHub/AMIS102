using AMIS.Modules.Chat.Contracts.v1.Messages;
using FluentValidation;

namespace AMIS.Modules.Chat.Features.v1.Messages.RemoveReaction;

public sealed class RemoveReactionCommandValidator : AbstractValidator<RemoveReactionCommand>
{
    public RemoveReactionCommandValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.Emoji).NotEmpty().MaximumLength(32);
    }
}
