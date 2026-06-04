using FluentValidation;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.CreatePurchaseOrdersFromCanvass;

public sealed class CreatePurchaseOrdersFromCanvassCommandValidator
    : AbstractValidator<CreatePurchaseOrdersFromCanvassCommand>
{
    public CreatePurchaseOrdersFromCanvassCommandValidator()
    {
        RuleFor(x => x.CanvassRequestId).NotEmpty();
        RuleFor(x => x.PlaceOfDelivery).NotEmpty().MaximumLength(256);
        RuleFor(x => x.DeliveryTerm).NotEmpty().MaximumLength(256);
        RuleFor(x => x.PaymentTerm).NotEmpty().MaximumLength(256);
        RuleFor(x => x.FundCluster).MaximumLength(64);
        RuleFor(x => x.OursBursNumber).MaximumLength(64);
    }
}
