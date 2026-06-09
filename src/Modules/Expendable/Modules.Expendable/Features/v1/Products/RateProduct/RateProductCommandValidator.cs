using AMIS.Modules.Expendable.Contracts.v1.Products;
using FluentValidation;

namespace AMIS.Modules.Expendable.Features.v1.Products.RateProduct;

public sealed class RateProductCommandValidator : AbstractValidator<RateProductCommand>
{
    public RateProductCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required");

        RuleFor(x => x.Value)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5");
    }
}
