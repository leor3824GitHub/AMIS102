using FluentValidation;
using FSH.Modules.Expendable.Contracts.v1.Requests;

namespace FSH.Modules.Expendable.Features.v1.Requests.ApproveSupplyRequest;

public sealed class ApproveSupplyRequestCommandValidator : AbstractValidator<ApproveSupplyRequestCommand>
{
    public ApproveSupplyRequestCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Request ID is required");

        RuleFor(x => x.WarehouseLocationId)
            .NotEmpty().WithMessage("Warehouse location is required when approving a supply request");

        RuleFor(x => x.ApprovedQuantities)
            .NotNull().WithMessage("Approved quantities are required")
            .NotEmpty().WithMessage("At least one item must be approved");

        RuleForEach(x => x.ApprovedQuantities)
            .Must(kvp => kvp.Value >= 0).WithMessage("Approved quantity cannot be negative");
    }
}
