using AMIS.Modules.Chat.Contracts.v1.Messages;
using FluentValidation;

namespace AMIS.Modules.Chat.Features.v1.Messages.DeleteMessage;

public sealed class DeleteMessageCommandValidator : AbstractValidator<DeleteMessageCommand>
{
    public DeleteMessageCommandValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
    }
}
