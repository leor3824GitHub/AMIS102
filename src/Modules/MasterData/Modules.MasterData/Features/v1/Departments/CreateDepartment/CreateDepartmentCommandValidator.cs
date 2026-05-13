using FluentValidation;
using AMIS.Modules.MasterData.Contracts.v1.References;

namespace AMIS.Modules.MasterData.Features.v1.Departments.CreateDepartment;

public sealed class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Department code is required")
            .MaximumLength(32).WithMessage("Department code must not exceed 32 characters");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Department name is required")
            .MaximumLength(160).WithMessage("Department name must not exceed 160 characters");

        RuleFor(x => x.Description)
            .MaximumLength(400).WithMessage("Description must not exceed 400 characters");
    }
}
