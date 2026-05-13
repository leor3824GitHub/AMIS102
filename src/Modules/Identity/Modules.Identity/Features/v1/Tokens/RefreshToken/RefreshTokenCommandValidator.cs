using FluentValidation;
using AMIS.Modules.Identity.Contracts.v1.Tokens.RefreshToken;

namespace AMIS.Modules.Identity.Features.v1.Tokens.RefreshToken;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(p => p.Token)
            .Cascade(CascadeMode.Stop)
            .NotEmpty();

        RuleFor(p => p.RefreshToken)
            .Cascade(CascadeMode.Stop)
            .NotEmpty();
    }
}


