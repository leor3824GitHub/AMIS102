using AMIS.Framework.Persistence;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.Expendable.Contracts.v1.Products;
using AMIS.Modules.Expendable.Data;
using AMIS.Modules.Expendable.Domain.Products;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Products;

public sealed class ListActiveProductsQueryHandler : IQueryHandler<ListActiveProductsQuery, PagedResponse<ProductDto>>
{
    private readonly ExpendableDbContext _dbContext;

    public ListActiveProductsQueryHandler(ExpendableDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<PagedResponse<ProductDto>> Handle(ListActiveProductsQuery query, CancellationToken cancellationToken)
    {
        var productQuery = _dbContext.Products.AsNoTracking()
            .Where(p => p.Status == ProductStatus.Active)
            .OrderBy(p => p.Name);

        var projected = productQuery.Select(p => p.ToProductDto());
        return await projected.ToPagedResponseAsync(query, cancellationToken).ConfigureAwait(false);
    }
}



