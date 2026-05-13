using FluentValidation;
using AMIS.Modules.Expendable.Contracts.v1.Cart;

namespace AMIS.Modules.Expendable.Features.v1.Cart.ClearCart;

public sealed class ClearCartCommandValidator : AbstractValidator<ClearCartCommand>
{
    public ClearCartCommandValidator()
    {
        RuleFor(x => x.CartId)
            .NotEmpty().WithMessage("Cart ID is required");
    }
}

