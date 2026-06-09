using AMIS.Modules.Expendable.Contracts.v1.Products;
using AMIS.Modules.Expendable.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Products.GetProductRatingSummary;

public sealed class GetProductRatingSummaryQueryHandler
    : IQueryHandler<GetProductRatingSummaryQuery, ProductRatingSummaryDto>
{
    private readonly ExpendableDbContext _dbContext;

    public GetProductRatingSummaryQueryHandler(ExpendableDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<ProductRatingSummaryDto> Handle(
        GetProductRatingSummaryQuery query,
        CancellationToken cancellationToken)
    {
        // Tenant filter is applied automatically (IsMultiTenant).
        var summary = await _dbContext.ProductRatings
            .AsNoTracking()
            .Where(r => r.ProductId == query.ProductId)
            .GroupBy(r => r.ProductId)
            .Select(g => new ProductRatingSummaryDto(
                g.Key,
                g.Average(x => (double)x.Value),
                g.Count()))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return summary ?? new ProductRatingSummaryDto(query.ProductId, 0d, 0);
    }
}
