using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Modules.Expendable.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Reports.GetPhysicalCountReport;

public sealed class GetPhysicalCountReportQueryHandler
    : IQueryHandler<GetPhysicalCountReportQuery, List<PhysicalCountGroupDto>>
{
    private const string UnclassifiedArticle = "(Unclassified)";

    private readonly ExpendableDbContext _dbContext;

    public GetPhysicalCountReportQueryHandler(ExpendableDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<List<PhysicalCountGroupDto>> Handle(
        GetPhysicalCountReportQuery query, CancellationToken cancellationToken)
    {
        var products = await _dbContext.Products
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.StockNo)
            .Select(p => new { p.Id, p.StockNo, p.Name, p.UnitOfMeasure, p.UnitPrice, p.Article })
            .ToListAsync(cancellationToken);

        var inventoryQuery = _dbContext.ProductInventories.AsNoTracking();

        if (query.WarehouseLocationId.HasValue && query.WarehouseLocationId != Guid.Empty)
            inventoryQuery = inventoryQuery.Where(pi => pi.WarehouseLocationId == query.WarehouseLocationId);

        var inventories = await inventoryQuery
            .Select(pi => new
            {
                pi.ProductId,
                QuantityOnHand = pi.QuantityAvailable + pi.QuantityReserved,
                pi.TotalValue,
                pi.AverageUnitPrice
            })
            .ToListAsync(cancellationToken);

        var inventoryByProduct = inventories
            .GroupBy(i => i.ProductId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    QuantityOnHand = g.Sum(i => i.QuantityOnHand),
                    TotalValue = g.Sum(i => i.TotalValue),
                    AverageUnitPrice = g.Sum(i => i.QuantityOnHand) > 0
                        ? Math.Round(g.Sum(i => i.TotalValue) / g.Sum(i => i.QuantityOnHand), 4)
                        : 0m
                });

        var items = products.Select(p =>
        {
            var inv = inventoryByProduct.GetValueOrDefault(p.Id);
            var unitValue = inv?.AverageUnitPrice > 0 ? inv.AverageUnitPrice : 0m;
            var balancePerCard = inv?.QuantityOnHand ?? 0;
            var onHandPerCount = balancePerCard; // system-derived — physical count matches records

            var shortageQty = balancePerCard - onHandPerCount;
            var shortageValue = shortageQty != 0 ? Math.Round(Math.Abs(shortageQty) * unitValue, 2) : 0m;

            var article = string.IsNullOrWhiteSpace(p.Article) ? UnclassifiedArticle : p.Article;

            return new
            {
                Article = article,
                Item = new PhysicalCountItemDto(
                    p.Name,
                    p.StockNo,
                    p.UnitOfMeasure,
                    unitValue,
                    balancePerCard,
                    onHandPerCount,
                    shortageQty,
                    shortageValue,
                    null)
            };
        });

        // Group by Article; classified articles alphabetically first, "(Unclassified)" last.
        // Items keep their StockNo ordering within each group (GroupBy is stable).
        return items
            .GroupBy(x => x.Article)
            .Select(g => new PhysicalCountGroupDto(g.Key, g.Select(x => x.Item).ToList()))
            .OrderBy(g => g.Article == UnclassifiedArticle)
            .ThenBy(g => g.Article, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

