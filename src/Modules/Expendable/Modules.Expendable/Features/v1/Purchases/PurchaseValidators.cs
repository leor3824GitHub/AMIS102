using FluentValidation;
using AMIS.Modules.Expendable.Contracts.v1.Purchases;

namespace AMIS.Modules.Expendable.Features.v1.Purchases;

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

public sealed class AddPurchaseLineItemCommandValidator : AbstractValidator<AddPurchaseLineItemCommand>
{
    public AddPurchaseLineItemCommandValidator()
    {
        RuleFor(x => x.PurchaseId)
            .NotEmpty().WithMessage("Purchase ID is required");

        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required");

        RuleFor(x => x.ProductCode)
            .NotEmpty().WithMessage("Product code is required")
            .MaximumLength(50).WithMessage("Product code must not exceed 50 characters");

        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("Product name is required")
            .MaximumLength(200).WithMessage("Product name must not exceed 200 characters");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero");

        RuleFor(x => x.UnitPrice)
            .GreaterThan(0).WithMessage("Unit price must be greater than zero");
    }
}

public sealed class RecordPurchaseReceiptCommandValidator : AbstractValidator<RecordPurchaseReceiptCommand>
{
    public RecordPurchaseReceiptCommandValidator()
    {
        RuleFor(x => x.PurchaseId)
            .NotEmpty().WithMessage("Purchase ID is required");

        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required");

        RuleFor(x => x.ReceivedQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Received quantity cannot be negative");

        RuleFor(x => x.RejectedQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Rejected quantity cannot be negative");

        RuleFor(x => x)
            .Must(x => x.ReceivedQuantity + x.RejectedQuantity > 0)
            .WithMessage("At least one unit must be recorded in receipt.");
    }
}

public sealed class RemovePurchaseLineItemCommandValidator : AbstractValidator<RemovePurchaseLineItemCommand>
{
    public RemovePurchaseLineItemCommandValidator()
    {
        RuleFor(x => x.PurchaseId)
            .NotEmpty().WithMessage("Purchase ID is required");

        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required");
    }
}

public sealed class SubmitPurchaseOrderCommandValidator : AbstractValidator<SubmitPurchaseOrderCommand>
{
    public SubmitPurchaseOrderCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Purchase ID is required");
    }
}

public sealed class ApprovePurchaseOrderCommandValidator : AbstractValidator<ApprovePurchaseOrderCommand>
{
    public ApprovePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Purchase ID is required");
    }
}

public sealed class CancelPurchaseOrderCommandValidator : AbstractValidator<CancelPurchaseOrderCommand>
{
    public CancelPurchaseOrderCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Purchase ID is required");
    }
}


