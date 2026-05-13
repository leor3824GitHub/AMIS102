using FluentValidation;
using AMIS.Modules.Expendable.Contracts.v1.Requests;

namespace AMIS.Modules.Expendable.Features.v1.Requests;

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

public sealed class AddSupplyRequestItemCommandValidator : AbstractValidator<AddSupplyRequestItemCommand>
{
    public AddSupplyRequestItemCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty().WithMessage("Request ID is required");

        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes must not exceed 500 characters");
    }
}

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


