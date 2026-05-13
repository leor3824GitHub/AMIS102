using FluentValidation;
using AMIS.Modules.MasterData.Contracts.v1.References;

namespace AMIS.Modules.MasterData.Features.v1.Positions.UpdatePosition;

public sealed class UpdatePositionCommandValidator : AbstractValidator<UpdatePositionCommand>
{
    public UpdatePositionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Position id is required");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Position code is required")
            .MaximumLength(32).WithMessage("Position code must not exceed 32 characters");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Position name is required")
            .MaximumLength(160).WithMessage("Position name must not exceed 160 characters");

        RuleFor(x => x.Description)
            .MaximumLength(400).WithMessage("Description must not exceed 400 characters");
    }
}
