using FSH.Modules.Expendable.Contracts.v1.Warehouse;
using FSH.Modules.Expendable.Data;
using FSH.Modules.Expendable.Domain.Purchases;
using FSH.Modules.Expendable.Domain.Requests;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Expendable.Features.v1.Reports.GetStockCard;

public sealed class GetStockCardQueryHandler : IQueryHandler<GetStockCardQuery, StockCardDto?>
{
    private readonly ExpendableDbContext _dbContext;

    public GetStockCardQueryHandler(ExpendableDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<StockCardDto?> Handle(GetStockCardQuery query, CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .Where(p => p.Id == query.ProductId && !p.IsDeleted)
            .Select(p => new { p.Id, p.SKU, p.Name, p.UnitOfMeasure })
            .FirstOrDefaultAsync(cancellationToken);

        if (product is null)
            return null;

        // --- RECEIPTS: accepted purchase inspections for this product ---
        var inspections = await _dbContext.PurchaseInspections
            .AsNoTracking()
            .Where(pi => pi.ProductId == query.ProductId
                      && pi.QuantityAccepted > 0
                      && !pi.IsDeleted)
            .OrderBy(pi => pi.InspectionDate)
            .Select(pi => new { pi.PurchaseId, pi.InspectionDate, pi.QuantityAccepted })
            .ToListAsync(cancellationToken);

        // Load the relevant purchases (for PO# and unit price from line items)
        var purchaseIds = inspections.Select(i => i.PurchaseId).Distinct().ToList();

        var purchases = await _dbContext.Purchases
            .AsNoTracking()
            .Where(p => purchaseIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        var purchaseMap = purchases.ToDictionary(p => p.Id);

        // --- ISSUANCES: fulfilled supply request items for this product ---
        // Load all fulfilled requests, then filter in memory (Items is a JSON-owned collection)
        var fulfilledRequests = await _dbContext.SupplyRequests
            .AsNoTracking()
            .Where(r => r.Status == SupplyRequestStatus.Fulfilled)
            .OrderBy(r => r.LastModifiedOnUtc)
            .ToListAsync(cancellationToken);

        // Keep only those that issued this product
        var issuanceRows = fulfilledRequests
            .Select(r => new
            {
                r.RequestNumber,
                r.DepartmentId,
                FulfilledOn = r.LastModifiedOnUtc ?? r.CreatedOnUtc,
                Item = r.Items.FirstOrDefault(i => i.ProductId == query.ProductId && i.FulfilledQuantity > 0)
            })
            .Where(x => x.Item != null)
            .ToList();

        // --- BUILD UNIFIED TRANSACTION LIST ---
        var transactions = new List<(DateTimeOffset Date, string Reference, string Type, string? Office,
            int Qty, decimal UnitCost, decimal TotalCost)>();

        foreach (var insp in inspections)
        {
            var purchase = purchaseMap.GetValueOrDefault(insp.PurchaseId);
            var lineItem = purchase?.LineItems.FirstOrDefault(li => li.ProductId == query.ProductId);
            var unitPrice = lineItem?.UnitPrice ?? 0m;
            var total = Math.Round(insp.QuantityAccepted * unitPrice, 2);

            transactions.Add((
                insp.InspectionDate,
                purchase?.PurchaseOrderNumber ?? $"PO-{insp.PurchaseId.ToString()[..8]}",
                "Receipt",
                null,
                insp.QuantityAccepted,
                unitPrice,
                total
            ));
        }

        foreach (var row in issuanceRows)
        {
            var item = row.Item!;
            var unitCost = item.FulfilledQuantity > 0
                ? Math.Round(item.FulfilledValue / item.FulfilledQuantity, 4)
                : 0m;

            transactions.Add((
                row.FulfilledOn,
                row.RequestNumber,
                "Issue",
                row.DepartmentId,
                item.FulfilledQuantity,
                unitCost,
                item.FulfilledValue
            ));
        }

        // Sort all transactions chronologically
        transactions = [.. transactions.OrderBy(t => t.Date)];

        // --- COMPUTE RUNNING BALANCE (moving-average cost) ---
        var runningQty = 0;
        var runningValue = 0m;
        var lines = new List<StockCardLineDto>();

        foreach (var tx in transactions)
        {
            int receiptQty = 0;
            decimal receiptUnitCost = 0m;
            decimal receiptTotalCost = 0m;
            int issueQty = 0;
            decimal issueUnitCost = 0m;
            decimal issueTotalCost = 0m;

            if (tx.Type == "Receipt")
            {
                receiptQty = tx.Qty;
                receiptUnitCost = tx.UnitCost;
                receiptTotalCost = tx.TotalCost;
                runningQty += tx.Qty;
                runningValue = Math.Round(runningValue + tx.TotalCost, 2);
            }
            else
            {
                issueQty = tx.Qty;
                issueUnitCost = tx.UnitCost;
                issueTotalCost = tx.TotalCost;
                runningQty = Math.Max(0, runningQty - tx.Qty);
                runningValue = Math.Max(0m, Math.Round(runningValue - tx.TotalCost, 2));
            }

            var balanceUnitCost = runningQty > 0
                ? Math.Round(runningValue / runningQty, 4)
                : 0m;

            lines.Add(new StockCardLineDto(
                tx.Date,
                tx.Reference,
                tx.Type,
                tx.Office,
                receiptQty,
                receiptUnitCost,
                receiptTotalCost,
                issueQty,
                issueUnitCost,
                issueTotalCost,
                runningQty,
                balanceUnitCost,
                runningValue
            ));
        }

        return new StockCardDto(
            product.Id,
            product.SKU,
            product.Name,
            product.UnitOfMeasure,
            lines
        );
    }
}
