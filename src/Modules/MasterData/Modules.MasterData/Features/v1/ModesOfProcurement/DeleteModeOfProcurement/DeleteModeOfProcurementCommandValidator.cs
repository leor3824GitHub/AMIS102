using FluentValidation;

namespace FSH.Modules.MasterData.Features.v1.ModesOfProcurement.DeleteModeOfProcurement;

public sealed class DeleteModeOfProcurementCommandValidator : AbstractValidator<DeleteModeOfProcurementCommand>
{
    public DeleteModeOfProcurementCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Mode of procurement id is required");
    }
}
