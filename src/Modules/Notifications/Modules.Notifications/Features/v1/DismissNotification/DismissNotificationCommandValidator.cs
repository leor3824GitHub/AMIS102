using AMIS.Modules.Notifications.Contracts.v1.Notifications;
using FluentValidation;

namespace AMIS.Modules.Notifications.Features.v1.DismissNotification;

public sealed class DismissNotificationCommandValidator : AbstractValidator<DismissNotificationCommand>
{
    public DismissNotificationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
