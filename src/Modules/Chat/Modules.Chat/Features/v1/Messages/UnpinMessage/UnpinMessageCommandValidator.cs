using AMIS.Modules.Chat.Contracts.v1.Messages;
using FluentValidation;

namespace AMIS.Modules.Chat.Features.v1.Messages.UnpinMessage;

public sealed class UnpinMessageCommandValidator : AbstractValidator<UnpinMessageCommand>
{
    public UnpinMessageCommandValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
    }
}
