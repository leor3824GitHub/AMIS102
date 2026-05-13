using FluentValidation;
using AMIS.Modules.Identity.Contracts.v1.Users.DeleteUser;

namespace AMIS.Modules.Identity.Features.v1.Users.DeleteUser;

public sealed class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("User ID is required.");
    }
}

