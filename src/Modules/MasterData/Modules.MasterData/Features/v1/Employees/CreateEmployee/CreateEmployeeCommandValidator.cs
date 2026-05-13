using FluentValidation;
using AMIS.Modules.MasterData.Contracts.v1.References;

namespace AMIS.Modules.MasterData.Features.v1.Employees.CreateEmployee;

public sealed class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.EmployeeNumber)
            .NotEmpty().WithMessage("Employee number is required")
            .MaximumLength(32).WithMessage("Employee number must not exceed 32 characters");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(128).WithMessage("First name must not exceed 128 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(128).WithMessage("Last name must not exceed 128 characters");

        RuleFor(x => x.WorkEmail)
            .MaximumLength(256).WithMessage("Work email must not exceed 256 characters")
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.WorkEmail)).WithMessage("Work email is invalid");

        RuleFor(x => x.IdentityUserId)
            .MaximumLength(64).WithMessage("Identity user id must not exceed 64 characters");

        RuleFor(x => x.OfficeId)
            .NotEmpty().WithMessage("Office id is required");

        RuleFor(x => x.DepartmentId)
            .NotEmpty().WithMessage("Department id is required");

        RuleFor(x => x.PositionId)
            .NotEmpty().WithMessage("Position id is required");
    }
}
