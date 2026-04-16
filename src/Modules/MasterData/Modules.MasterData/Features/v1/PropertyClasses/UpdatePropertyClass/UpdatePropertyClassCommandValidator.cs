using FluentValidation;
using FSH.Modules.MasterData.Contracts.v1.PropertyClasses;

namespace FSH.Modules.MasterData.Features.v1.PropertyClasses.UpdatePropertyClass;

public sealed class UpdatePropertyClassCommandValidator : AbstractValidator<UpdatePropertyClassCommand>
{
    public UpdatePropertyClassCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}
