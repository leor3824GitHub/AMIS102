using AMIS.Modules.Expendable.Contracts.v1.Products;
using AMIS.Modules.Expendable.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Products.GetProductsByIds;

public sealed class GetProductsByIdsQueryHandler : IQueryHandler<GetProductsByIdsQuery, IReadOnlyList<ProductDto>>
{
    private readonly ExpendableDbContext _dbContext;

    public GetProductsByIdsQueryHandler(ExpendableDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<IReadOnlyList<ProductDto>> Handle(GetProductsByIdsQuery query, CancellationToken cancellationToken)
    {
        if (query.Ids is null || query.Ids.Count == 0)
            return [];

        var ids = query.Ids.Distinct().ToList();

        var products = await _dbContext.Products
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return products.Select(p => p.ToProductDto()).ToList();
    }
}
