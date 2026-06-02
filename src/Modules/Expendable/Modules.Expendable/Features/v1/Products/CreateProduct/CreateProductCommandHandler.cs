using AMIS.Framework.Core.Context;
using AMIS.Modules.Expendable.Contracts.v1.Products;
using AMIS.Modules.Expendable.Data;
using AMIS.Modules.Expendable.Domain.Products;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Products.CreateProduct;

public sealed class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, ProductDto>
{
    private readonly ExpendableDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreateProductCommandHandler(ExpendableDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ProductDto> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var stockNoInUse = await _dbContext.Products
            .IgnoreQueryFilters()
            .AnyAsync(p => p.TenantId == (_currentUser.GetTenant() ?? string.Empty) && p.StockNo == command.StockNo, cancellationToken)
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
                .FirstOrDefaultAsync(p => p.Id == command.ParentProductId && p.TenantId == (_currentUser.GetTenant() ?? string.Empty), cancellationToken)
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
                _currentUser.GetTenant() ?? throw new InvalidOperationException("Tenant ID required"),
                command.StockNo,
                command.Article,
                command.Name,
                command.Description,
                command.UnitPrice,
                command.UnitOfMeasure,
                command.MinimumStockLevel,
                command.ReorderQuantity,
                command.CategoryId,
                command.SupplierId,
                command.ImageUrl);

            product.CreatedBy = _currentUser.GetUserId().ToString();
        }

        _dbContext.Products.Add(product);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when ((ex.InnerException?.Message?.Contains("IX_Products_TenantId_StockNo", StringComparison.OrdinalIgnoreCase) ?? false)
            || (ex.InnerException?.Message?.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.StockNo), "A product with this Stock No. already exists.")
            ]);
        }

        return product.ToProductDto();
    }
}

