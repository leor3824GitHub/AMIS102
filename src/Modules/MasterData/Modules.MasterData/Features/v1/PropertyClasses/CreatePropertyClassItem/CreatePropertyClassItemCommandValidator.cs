using FluentValidation;
using FSH.Modules.MasterData.Contracts.v1.PropertyClasses;

namespace FSH.Modules.MasterData.Features.v1.PropertyClasses.CreatePropertyClassItem;

public sealed class CreatePropertyClassItemCommandValidator : AbstractValidator<CreatePropertyClassItemCommand>
{
    public CreatePropertyClassItemCommandValidator()
    {
        RuleFor(x => x.PropertyClassId).NotEmpty();
        RuleFor(x => x.ItemCode).NotEmpty().MaximumLength(2)
            .Matches("^[A-Za-z0-9]+$").WithMessage("Item code must be alphanumeric.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}
