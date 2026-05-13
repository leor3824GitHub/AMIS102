using FluentValidation;
using AMIS.Modules.Expendable.Contracts.v1.Requests;

namespace AMIS.Modules.Expendable.Features.v1.Requests.CreateSupplyRequest;

public sealed class CreateSupplyRequestCommandValidator : AbstractValidator<CreateSupplyRequestCommand>
{
    public CreateSupplyRequestCommandValidator()
    {
        RuleFor(x => x.DepartmentId)
            .NotEmpty().WithMessage("Department ID is required")
            .MaximumLength(50).WithMessage("Department ID must not exceed 50 characters");

        RuleFor(x => x.BusinessJustification)
            .MaximumLength(1000).WithMessage("Business justification must not exceed 1000 characters");
    }
}

