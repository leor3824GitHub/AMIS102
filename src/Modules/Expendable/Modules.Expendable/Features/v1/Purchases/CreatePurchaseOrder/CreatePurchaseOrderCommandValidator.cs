using FluentValidation;
using AMIS.Modules.Expendable.Contracts.v1.Purchases;

namespace AMIS.Modules.Expendable.Features.v1.Purchases.CreatePurchaseOrder;

public sealed class CreatePurchaseOrderCommandValidator : AbstractValidator<CreatePurchaseOrderCommand>
{
    public CreatePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.SupplierId)
            .NotEmpty().WithMessage("Supplier ID is required")
            .MaximumLength(50).WithMessage("Supplier ID must not exceed 50 characters");

        RuleFor(x => x.SupplierName)
            .NotEmpty().WithMessage("Supplier name is required")
            .MaximumLength(200).WithMessage("Supplier name must not exceed 200 characters");

        RuleFor(x => x.WarehouseLocationId)
            .NotEmpty().WithMessage("Warehouse location is required");

        RuleFor(x => x.WarehouseLocationName)
            .NotEmpty().WithMessage("Warehouse location name is required")
            .MaximumLength(200).WithMessage("Warehouse location name must not exceed 200 characters");
    }
}

