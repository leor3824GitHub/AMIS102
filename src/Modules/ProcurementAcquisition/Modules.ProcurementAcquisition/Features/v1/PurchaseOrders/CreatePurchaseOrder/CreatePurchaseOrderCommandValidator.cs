using FluentValidation;
using FSH.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;

namespace FSH.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.CreatePurchaseOrder;

public sealed class CreatePurchaseOrderCommandValidator : AbstractValidator<CreatePurchaseOrderCommand>
{
    public CreatePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.PurchaseRequestId).NotEmpty();
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.SupplierName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.SupplierAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.SupplierTin).MaximumLength(32);
        RuleFor(x => x.PlaceOfDelivery).NotEmpty().MaximumLength(256);
        RuleFor(x => x.DeliveryTerm).NotEmpty().MaximumLength(256);
        RuleFor(x => x.PaymentTerm).NotEmpty().MaximumLength(256);
        RuleFor(x => x.LineItems).NotEmpty().WithMessage("At least one line item is required.");
        RuleForEach(x => x.LineItems).ChildRules(li =>
        {
            li.RuleFor(x => x.Unit).NotEmpty().MaximumLength(64);
            li.RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
            li.RuleFor(x => x.Quantity).GreaterThan(0);
            li.RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0);
        });
    }
}
