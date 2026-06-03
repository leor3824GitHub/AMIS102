using FluentValidation;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.IssuePurchaseOrder;

public sealed class IssuePurchaseOrderCommandValidator : AbstractValidator<IssuePurchaseOrderCommand>
{
    public IssuePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
