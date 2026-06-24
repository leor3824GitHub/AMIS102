using AMIS.Modules.Chat.Contracts.v1.Messages;
using FluentValidation;

namespace AMIS.Modules.Chat.Features.v1.Messages.PinMessage;

public sealed class PinMessageCommandValidator : AbstractValidator<PinMessageCommand>
{
    public PinMessageCommandValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
    }
}
