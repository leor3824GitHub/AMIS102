using FluentValidation;
using AMIS.Modules.Expendable.Contracts.v1.Purchases;

namespace AMIS.Modules.Expendable.Features.v1.Purchases.RecordPurchaseReceipt;

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

