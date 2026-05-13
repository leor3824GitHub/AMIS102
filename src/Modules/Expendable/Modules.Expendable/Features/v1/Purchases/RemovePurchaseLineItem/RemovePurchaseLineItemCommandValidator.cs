using FluentValidation;
using AMIS.Modules.Expendable.Contracts.v1.Purchases;

namespace AMIS.Modules.Expendable.Features.v1.Purchases.RemovePurchaseLineItem;

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

