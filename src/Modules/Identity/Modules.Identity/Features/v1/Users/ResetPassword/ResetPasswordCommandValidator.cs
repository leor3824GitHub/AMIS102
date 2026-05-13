using FluentValidation;
using AMIS.Modules.Identity.Contracts.v1.Users.ResetPassword;

namespace AMIS.Modules.Identity.Features.v1.Users.ResetPassword;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.Token).NotEmpty();
    }
}
