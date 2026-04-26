using FluentValidation;
using FSH.Modules.AssetProcurement.Contracts.v1.AssetPurchaseOrders;

namespace FSH.Modules.AssetProcurement.Features.v1.AssetPurchaseOrders.CreateAssetPurchaseOrder;

public sealed class CreateAssetPurchaseOrderCommandValidator : AbstractValidator<CreateAssetPurchaseOrderCommand>
{
    public CreateAssetPurchaseOrderCommandValidator()
    {
        RuleFor(x => x.PurchaseRequestId).NotEmpty();
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.SupplierName).NotEmpty().MaximumLength(500);
        RuleFor(x => x.SupplierAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.PlaceOfDelivery).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DeliveryTerm).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PaymentTerm).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LineItems).NotEmpty().WithMessage("At least one line item is required.");
        RuleForEach(x => x.LineItems).ChildRules(li =>
        {
            li.RuleFor(x => x.Unit).NotEmpty().MaximumLength(64);
            li.RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
            li.RuleFor(x => x.Quantity).GreaterThan(0);
            li.RuleFor(x => x.UnitCost).GreaterThan(0);
        });
    }
}
