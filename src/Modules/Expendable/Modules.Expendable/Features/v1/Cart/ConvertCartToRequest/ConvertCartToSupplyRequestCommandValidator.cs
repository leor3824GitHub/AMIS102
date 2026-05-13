using FluentValidation;
using AMIS.Modules.Expendable.Contracts.v1.Cart;

namespace AMIS.Modules.Expendable.Features.v1.Cart.ConvertCartToRequest;

public sealed class ConvertCartToSupplyRequestCommandValidator : AbstractValidator<ConvertCartToSupplyRequestCommand>
{
    public ConvertCartToSupplyRequestCommandValidator()
    {
        RuleFor(x => x.CartId)
            .NotEmpty().WithMessage("Cart ID is required");

        RuleFor(x => x.DepartmentId)
            .NotEmpty().WithMessage("Department ID is required");

        RuleFor(x => x.BusinessJustification)
            .MaximumLength(1000).WithMessage("Business justification must not exceed 1000 characters");
    }
}

