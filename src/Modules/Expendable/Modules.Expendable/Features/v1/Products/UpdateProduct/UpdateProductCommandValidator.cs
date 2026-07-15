using FluentValidation;
using AMIS.Modules.Expendable.Contracts.v1.Products;

namespace AMIS.Modules.Expendable.Features.v1.Products.UpdateProduct;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Product ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required")
            .MaximumLength(255).WithMessage("Product name must not exceed 255 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");

        RuleFor(x => x.UnitPrice)
            .GreaterThan(0).WithMessage("Unit price must be greater than zero");

        RuleFor(x => x.MinimumStockLevel)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum stock level cannot be negative");

        RuleFor(x => x.ReorderQuantity)
            .GreaterThan(0).WithMessage("Reorder quantity must be greater than zero");

        // ImageUrl carries one of: a base64 data URL (new upload), null/blank (clear), or the existing
        // storage key echoed back (no change). The handler interprets these; here we only guard the size
        // of a base64 upload (the server re-encodes/downscales anyway).
        RuleFor(x => x.ImageUrl)
            .MaximumLength(10_000_000).WithMessage("Image data URL exceeds maximum size");
    }
}

