using AMIS.Framework.Core.Context;
using AMIS.Modules.Expendable.Contracts.v1.Products;
using AMIS.Modules.Expendable.Data;
using AMIS.Modules.Expendable.Data.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Products.UpdateProduct;

public sealed class UpdateProductCommandHandler : ICommandHandler<UpdateProductCommand, ProductDto>
{
    private readonly ExpendableDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ProductImageStorage _imageStorage;

    public UpdateProductCommandHandler(ExpendableDbContext dbContext, ICurrentUser currentUser, ProductImageStorage imageStorage)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _imageStorage = imageStorage;
    }

    public async ValueTask<ProductDto> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Product {command.Id} not found.");

        product.Update(
            command.Name,
            command.Description,
            command.Article,
            command.UnitPrice,
            command.MinimumStockLevel,
            command.ReorderQuantity);

        if (command.VariantName is not null)
        {
            product.SetVariantName(command.VariantName);
        }

        product.SetCategory(command.CategoryId);
        product.SetSupplier(command.SupplierId);

        // Image intent, mirroring UpdateAssetImage semantics — the field carries one of three signals:
        //   • a data:…;base64 URL  → a new upload: store full+thumbnail, remove the previous files
        //   • null / whitespace    → clear the photo, remove the previous files
        //   • any other string     → the existing storage key echoed back unchanged → leave as-is
        var oldImageKey = product.ImageUrl;
        var oldThumbnailKey = product.ThumbnailUrl;
        var imageBytes = ProductImageDataUrl.Decode(command.ImageUrl);
        (string ImageKey, string ThumbnailKey)? savedImage = null;
        var imageChangedOrCleared = false;

        if (imageBytes is not null)
        {
            savedImage = await _imageStorage.SaveAsync(imageBytes, product.TenantId, cancellationToken).ConfigureAwait(false);
            product.SetImage(savedImage.Value.ImageKey, savedImage.Value.ThumbnailKey);
            imageChangedOrCleared = true;
        }
        else if (string.IsNullOrWhiteSpace(command.ImageUrl))
        {
            product.ClearImage();
            imageChangedOrCleared = true;
        }

        product.LastModifiedBy = _currentUser.GetUserId().ToString();

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Row didn't persist → don't orphan the blobs we just wrote.
            if (savedImage is not null)
                await _imageStorage.RemoveAsync(savedImage.Value.ImageKey, savedImage.Value.ThumbnailKey, cancellationToken).ConfigureAwait(false);
            throw;
        }

        // After the commit succeeded, best-effort cleanup of the replaced/cleared photo's old files.
        if (imageChangedOrCleared)
            await _imageStorage.RemoveAsync(oldImageKey, oldThumbnailKey, cancellationToken).ConfigureAwait(false);

        return product.ToProductDto();
    }
}
