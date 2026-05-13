using FluentValidation;
using AMIS.Modules.MasterData.Contracts.v1.References;

namespace AMIS.Modules.MasterData.Features.v1.UnitOfMeasures.UpdateUnitOfMeasure;

public sealed class UpdateUnitOfMeasureCommandValidator : AbstractValidator<UpdateUnitOfMeasureCommand>
{
    public UpdateUnitOfMeasureCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Unit of measure id is required");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Unit of measure code is required")
            .MaximumLength(32).WithMessage("Unit of measure code must not exceed 32 characters");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Unit of measure name is required")
            .MaximumLength(160).WithMessage("Unit of measure name must not exceed 160 characters");

        RuleFor(x => x.Description)
            .MaximumLength(400).WithMessage("Description must not exceed 400 characters");
    }
}
