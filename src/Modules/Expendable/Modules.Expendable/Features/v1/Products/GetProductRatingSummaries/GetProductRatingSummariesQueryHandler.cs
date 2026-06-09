using AMIS.Modules.Expendable.Contracts.v1.Products;
using AMIS.Modules.Expendable.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Products.GetProductRatingSummaries;

public sealed class GetProductRatingSummariesQueryHandler
    : IQueryHandler<GetProductRatingSummariesQuery, List<ProductRatingSummaryDto>>
{
    private readonly ExpendableDbContext _dbContext;

    public GetProductRatingSummariesQueryHandler(ExpendableDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<List<ProductRatingSummaryDto>> Handle(
        GetProductRatingSummariesQuery query,
        CancellationToken cancellationToken)
    {
        // Tenant filter is applied automatically (IsMultiTenant).
        return await _dbContext.ProductRatings
            .AsNoTracking()
            .GroupBy(r => r.ProductId)
            .Select(g => new ProductRatingSummaryDto(
                g.Key,
                g.Average(x => (double)x.Value),
                g.Count()))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
