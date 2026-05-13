using FluentValidation;
using AMIS.Modules.MasterData.Contracts.v1.References;

namespace AMIS.Modules.MasterData.Features.v1.Offices.CreateOffice;

public sealed class CreateOfficeCommandValidator : AbstractValidator<CreateOfficeCommand>
{
    public CreateOfficeCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Office code is required")
            .MaximumLength(32).WithMessage("Office code must not exceed 32 characters");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Office name is required")
            .MaximumLength(160).WithMessage("Office name must not exceed 160 characters");

        RuleFor(x => x.Description)
            .MaximumLength(400).WithMessage("Description must not exceed 400 characters");
    }
}

