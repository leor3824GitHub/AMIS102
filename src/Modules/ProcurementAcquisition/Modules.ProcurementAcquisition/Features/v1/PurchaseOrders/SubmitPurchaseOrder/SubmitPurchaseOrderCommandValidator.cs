using FluentValidation;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.SubmitPurchaseOrder;

public sealed class SubmitPurchaseOrderCommandValidator : AbstractValidator<SubmitPurchaseOrderCommand>
{
    public SubmitPurchaseOrderCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
