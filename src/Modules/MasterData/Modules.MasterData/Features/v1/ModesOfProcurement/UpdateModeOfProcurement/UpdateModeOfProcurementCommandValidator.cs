using FluentValidation;

namespace AMIS.Modules.MasterData.Features.v1.ModesOfProcurement.UpdateModeOfProcurement;

public sealed class UpdateModeOfProcurementCommandValidator : AbstractValidator<UpdateModeOfProcurementCommand>
{
    public UpdateModeOfProcurementCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(250).WithMessage("Name must not exceed 250 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");
    }
}

