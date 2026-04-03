using FSH.Modules.Expendable.Contracts.v1.Requests;
using FSH.Modules.Expendable.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Expendable.Features.v1.Reports.GetPhysicalCountReport;

public sealed class GetPhysicalCountReportQueryHandler
    : IQueryHandler<GetPhysicalCountReportQuery, List<PhysicalCountItemDto>>
{
    private readonly ExpendableDbContext _dbContext;

    public GetPhysicalCountReportQueryHandler(ExpendableDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<List<PhysicalCountItemDto>> Handle(
        GetPhysicalCountReportQuery query, CancellationToken cancellationToken)
    {
        var products = await _dbContext.Products
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.SKU)
            .Select(p => new { p.Id, p.SKU, p.Name, p.UnitOfMeasure, p.UnitPrice })
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

        var items = products.Select((p, index) =>
        {
            var inv = inventoryByProduct.GetValueOrDefault(p.Id);
            var unitValue = inv?.AverageUnitPrice > 0 ? inv.AverageUnitPrice : 0m;
            var balancePerCard = inv?.QuantityOnHand ?? 0;
            var onHandPerCount = balancePerCard; // system-derived — physical count matches records

            var shortageQty = balancePerCard - onHandPerCount;
            var shortageValue = shortageQty != 0 ? Math.Round(Math.Abs(shortageQty) * unitValue, 2) : 0m;

            return new PhysicalCountItemDto(
                index + 1,
                p.Name,
                p.SKU,
                p.UnitOfMeasure,
                unitValue,
                balancePerCard,
                onHandPerCount,
                shortageQty,
                shortageValue,
                null
            );
        }).ToList();

        return items;
    }
}
