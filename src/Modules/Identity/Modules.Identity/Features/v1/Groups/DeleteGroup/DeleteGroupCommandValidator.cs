using FluentValidation;
using AMIS.Modules.Identity.Contracts.v1.Groups.DeleteGroup;

namespace AMIS.Modules.Identity.Features.v1.Groups.DeleteGroup;

public sealed class DeleteGroupCommandValidator : AbstractValidator<DeleteGroupCommand>
{
    public DeleteGroupCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Group ID is required.");
    }
}

