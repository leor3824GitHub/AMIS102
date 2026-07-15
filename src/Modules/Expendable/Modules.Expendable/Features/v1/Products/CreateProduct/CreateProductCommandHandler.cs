using AMIS.Framework.Core.Context;
using AMIS.Modules.Expendable.Contracts.v1.Products;
using AMIS.Modules.Expendable.Data;
using AMIS.Modules.Expendable.Data.Services;
using AMIS.Modules.Expendable.Domain.Products;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Products.CreateProduct;

public sealed class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, ProductDto>
{
    private readonly ExpendableDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ProductImageStorage _imageStorage;

    public CreateProductCommandHandler(ExpendableDbContext dbContext, ICurrentUser currentUser, ProductImageStorage imageStorage)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _imageStorage = imageStorage;
    }

    public async ValueTask<ProductDto> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.GetTenant() ?? throw new InvalidOperationException("Tenant ID required");

        var stockNoInUse = await _dbContext.Products
            .IgnoreQueryFilters()
            .AnyAsync(p => p.TenantId == tenantId && p.StockNo == command.StockNo, cancellationToken)
            .ConfigureAwait(false);

        if (stockNoInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.StockNo), "A product with this Stock No. already exists.")
            ]);
        }

        Product product;

        if (command.ParentProductId is not null)
        {
            var parent = await _dbContext.Products
                .FirstOrDefaultAsync(p => p.Id == command.ParentProductId && p.TenantId == tenantId, cancellationToken)
                .ConfigureAwait(false);

            if (parent is null)
            {
                throw new FluentValidation.ValidationException(
                [
                    new FluentValidation.Results.ValidationFailure(nameof(command.ParentProductId), "Parent product not found for this tenant.")
                ]);
            }

            if (string.IsNullOrWhiteSpace(command.VariantName))
            {
                throw new FluentValidation.ValidationException(
                [
                    new FluentValidation.Results.ValidationFailure(nameof(command.VariantName), "VariantName is required when ParentProductId is provided.")
                ]);
            }

            product = parent.CreateVariant(
                command.StockNo,
                command.VariantName!,
                command.UnitPrice,
                command.UnitOfMeasure,
                command.MinimumStockLevel,
                command.ReorderQuantity);

            product.CreatedBy = _currentUser.GetUserId().ToString();
        }
        else
        {
            product = Product.Create(
                tenantId,
                command.StockNo,
                command.Article,
                command.Name,
                command.Description,
                command.UnitPrice,
                command.UnitOfMeasure,
                command.MinimumStockLevel,
                command.ReorderQuantity,
                command.CategoryId,
                command.SupplierId);

            product.CreatedBy = _currentUser.GetUserId().ToString();
        }

        // Store an uploaded photo as files (full + thumbnail) and record the keys — never a base64 blob.
        // A non-data-URL value (there is none from the create flow) is ignored.
        var imageBytes = ProductImageDataUrl.Decode(command.ImageUrl);
        (string ImageKey, string ThumbnailKey)? savedImage = null;
        if (imageBytes is not null)
        {
            savedImage = await _imageStorage.SaveAsync(imageBytes, tenantId, cancellationToken).ConfigureAwait(false);
            product.SetImage(savedImage.Value.ImageKey, savedImage.Value.ThumbnailKey);
        }

        _dbContext.Products.Add(product);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when ((ex.InnerException?.Message?.Contains("IX_Products_TenantId_StockNo", StringComparison.OrdinalIgnoreCase) ?? false)
            || (ex.InnerException?.Message?.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            // Row didn't persist → don't orphan the blobs we just wrote.
            if (savedImage is not null)
                await _imageStorage.RemoveAsync(savedImage.Value.ImageKey, savedImage.Value.ThumbnailKey, cancellationToken).ConfigureAwait(false);

            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.StockNo), "A product with this Stock No. already exists.")
            ]);
        }

        return product.ToProductDto();
    }
}
