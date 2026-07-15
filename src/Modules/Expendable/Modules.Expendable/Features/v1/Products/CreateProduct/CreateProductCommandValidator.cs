using FluentValidation;
using AMIS.Modules.Expendable.Contracts.v1.Products;

namespace AMIS.Modules.Expendable.Features.v1.Products.CreateProduct;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.StockNo)
            .NotEmpty().WithMessage("Stock No. is required")
            .MaximumLength(50).WithMessage("Stock No. must not exceed 50 characters");

        RuleFor(x => x.Article)
            .NotEmpty().WithMessage("Article is required")
            .MaximumLength(100).WithMessage("Article must not exceed 100 characters");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required")
            .MaximumLength(255).WithMessage("Product name must not exceed 255 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");

        RuleFor(x => x.UnitPrice)
            .GreaterThan(0).WithMessage("Unit price must be greater than zero");

        RuleFor(x => x.UnitOfMeasure)
            .NotEmpty().WithMessage("Unit of measure is required")
            .MaximumLength(50).WithMessage("Unit of measure must not exceed 50 characters");

        RuleFor(x => x.MinimumStockLevel)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum stock level cannot be negative");

        RuleFor(x => x.ReorderQuantity)
            .GreaterThan(0).WithMessage("Reorder quantity must be greater than zero");

        // ImageUrl is an optional base64 data URL for the initial photo (stored as files by the handler);
        // only guard its size here — the server re-encodes/downscales anyway.
        RuleFor(x => x.ImageUrl)
            .MaximumLength(10_000_000).WithMessage("Image data URL exceeds maximum size");

        RuleFor(x => x.VariantName)
            .NotEmpty()
            .When(x => x.ParentProductId.HasValue)
            .WithMessage("Variant name is required when creating a product variant.");
    }
}

