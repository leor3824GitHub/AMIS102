using AMIS.Modules.Expendable.Contracts.v1.Products;
using AMIS.Modules.Expendable.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Products.GetProduct;

public sealed class GetProductQueryHandler : IQueryHandler<GetProductQuery, ProductDto?>
{
    private readonly ExpendableDbContext _dbContext;

    public GetProductQueryHandler(ExpendableDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<ProductDto?> Handle(GetProductQuery query, CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);
        return product?.ToProductDto();
    }
}

