using FluentValidation;
using AMIS.Modules.AssetRegister.Contracts.v1.Counting;

namespace AMIS.Modules.AssetRegister.Features.v1.Counting.ClosePhysicalCount;

public sealed class ClosePhysicalCountCommandValidator : AbstractValidator<ClosePhysicalCountCommand>
{
    public ClosePhysicalCountCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty().WithMessage("Physical count session ID is required.");
        RuleFor(x => x.ClosedOn).NotEqual(default(DateOnly)).WithMessage("Closure date must be provided.");
        RuleFor(x => x.ApprovedBy).NotNull().WithMessage("Approval by employee reference is required.");
        RuleFor(x => x.ApprovedBy.EmployeeId).NotEmpty().When(x => x.ApprovedBy is not null).WithMessage("Approver employee ID is required.");
        RuleFor(x => x.ApprovedBy.PrintedName).NotEmpty().When(x => x.ApprovedBy is not null).WithMessage("Approver printed name is required.");
    }
}

