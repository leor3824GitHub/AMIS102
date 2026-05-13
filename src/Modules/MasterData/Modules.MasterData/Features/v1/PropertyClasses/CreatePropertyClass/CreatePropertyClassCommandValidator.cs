using FluentValidation;
using AMIS.Modules.MasterData.Contracts.v1.PropertyClasses;

namespace AMIS.Modules.MasterData.Features.v1.PropertyClasses.CreatePropertyClass;

public sealed class CreatePropertyClassCommandValidator : AbstractValidator<CreatePropertyClassCommand>
{
    public CreatePropertyClassCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(4)
            .Matches("^[A-Za-z0-9]+$").WithMessage("Code must be alphanumeric.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}

