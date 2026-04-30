using FluentValidation;

namespace FSH.Modules.MasterData.Features.v1.ModesOfProcurement.CreateModeOfProcurement;

public sealed class CreateModeOfProcurementCommandValidator : AbstractValidator<CreateModeOfProcurementCommand>
{
    public CreateModeOfProcurementCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(250).WithMessage("Name must not exceed 250 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");
    }
}
