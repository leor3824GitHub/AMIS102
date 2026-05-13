using FluentValidation;
using AMIS.Modules.Expendable.Contracts.v1.Products;

namespace AMIS.Modules.Expendable.Features.v1.Products.CreateProduct;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    private static bool IsValidImageUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return true;

        // Accept data URLs (base64 encoded images)
        if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return true;

        // Accept absolute HTTP(S) URLs
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && 
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    public CreateProductCommandValidator()
    {
        RuleFor(x => x.SKU)
            .NotEmpty().WithMessage("SKU is required")
            .MaximumLength(50).WithMessage("SKU must not exceed 50 characters");

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

        RuleFor(x => x.ImageUrl)
            .MaximumLength(10_000_000).WithMessage("Image data URL exceeds maximum size")
            .Must(url => url == null || IsValidImageUrl(url))
            .WithMessage("Image URL must be a valid URL or data URL");

        RuleFor(x => x.VariantName)
            .NotEmpty()
            .When(x => x.ParentProductId.HasValue)
            .WithMessage("Variant name is required when creating a product variant.");
    }
}

