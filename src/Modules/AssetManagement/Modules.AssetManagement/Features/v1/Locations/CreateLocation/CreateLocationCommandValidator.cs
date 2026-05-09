using FluentValidation;

namespace FSH.Modules.AssetManagement.Features.v1.Locations.CreateLocation;

public sealed class CreateLocationCommandValidator : AbstractValidator<CreateLocationCommand>
{
    public CreateLocationCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().MaximumLength(64);

        RuleFor(x => x.Name)
            .NotEmpty().MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}
