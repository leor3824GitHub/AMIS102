using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Modules.Expendable.Data;
using AMIS.Modules.Expendable.Domain.Requests;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Reports.GetStockCard;

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
            .Select(p => new { p.Id, p.StockNo, p.Name, p.UnitOfMeasure })
            .FirstOrDefaultAsync(cancellationToken);

        if (product is null)
            return null;

        // --- RECEIPTS: accepted-stock batches from ProductInventory ---
        // Batches are a JSON-owned collection (cannot be filtered in SQL), so load the product's inventory
        // rows and flatten in memory. Each batch is one receipt; its SourceReference is the IAR number.
        var inventories = await _dbContext.ProductInventories
            .AsNoTracking()
            .Where(pi => pi.ProductId == query.ProductId && !pi.IsDeleted)
            .ToListAsync(cancellationToken);

        var receiptRows = inventories
            .SelectMany(pi => pi.Batches)
            .Select(b => new
            {
                Date = b.ReceivedDate,
                Reference = string.IsNullOrWhiteSpace(b.SourceReference)
                    ? $"IAR-{b.PurchaseId.ToString()[..8]}"
                    : b.SourceReference!,
                Qty = b.QuantityAvailable,
                UnitCost = b.UnitPrice
            })
            .ToList();

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

        foreach (var receipt in receiptRows)
        {
            var total = Math.Round(receipt.Qty * receipt.UnitCost, 2);
            transactions.Add((
                receipt.Date,
                receipt.Reference,
                "Receipt",
                null,
                receipt.Qty,
                receipt.UnitCost,
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
            product.StockNo,
            product.Name,
            product.UnitOfMeasure,
            lines
        );
    }
}
