using AMIS.Modules.Notifications.Contracts.v1.Notifications;
using FluentValidation;

namespace AMIS.Modules.Notifications.Features.v1.MarkRead;

public sealed class MarkNotificationReadCommandValidator : AbstractValidator<MarkNotificationReadCommand>
{
    public MarkNotificationReadCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
