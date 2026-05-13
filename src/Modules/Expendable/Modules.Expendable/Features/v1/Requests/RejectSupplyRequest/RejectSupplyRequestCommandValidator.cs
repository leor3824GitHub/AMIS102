using FluentValidation;
using AMIS.Modules.Expendable.Contracts.v1.Requests;

namespace AMIS.Modules.Expendable.Features.v1.Requests.RejectSupplyRequest;

public sealed class RejectSupplyRequestCommandValidator : AbstractValidator<RejectSupplyRequestCommand>
{
    public RejectSupplyRequestCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Request ID is required");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Rejection reason is required")
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters");
    }
}

