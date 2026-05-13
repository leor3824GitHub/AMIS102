using FluentValidation;

namespace AMIS.Modules.AssetManagement.Features.v1.Reclassification.ReclassifyProperties;

public sealed class ReclassifyPropertiesCommandValidator : AbstractValidator<ReclassifyPropertiesCommand>
{
    public ReclassifyPropertiesCommandValidator()
    {
        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Notes must not exceed 500 characters.");
    }
}

