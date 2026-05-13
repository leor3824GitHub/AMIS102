using FluentValidation;
using AMIS.Modules.Expendable.Contracts.v1.Purchases;

namespace AMIS.Modules.Expendable.Features.v1.Purchases.CancelPurchaseOrder;

public sealed class CancelPurchaseOrderCommandValidator : AbstractValidator<CancelPurchaseOrderCommand>
{
    public CancelPurchaseOrderCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Purchase ID is required");
    }
}

