using System.Net;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Domain.PurchaseOrders;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders;

/// <summary>
/// Shared logic for turning incoming PO line requests into domain line data. PO lines originate from the
/// awarded canvass quotation, which carries only Description/Qty/Price — not the screened StockNumber or
/// CatalogItemId. Both the create and update paths must recover those from the source PR (matched by
/// description) and then guard Supply POs against any line that still can't resolve a StockNumber.
/// </summary>
internal static class PurchaseOrderLineResolver
{
    /// <summary>Snapshot of a source PR line, used to recover StockNumber/CatalogItemId onto PO lines.</summary>
    internal readonly record struct SourcePrLine(string ItemDescription, string? StockNumber, Guid? CatalogItemId);

    /// <summary>
    /// Loads the source PR's category and line snapshots. Returns <see cref="ProcurementCategory.Asset"/> with
    /// no lines when the PR can't be found (the caller keeps its own default behaviour for a missing PR).
    /// </summary>
    internal static async Task<(ProcurementCategory Category, IReadOnlyList<SourcePrLine> Lines)> LoadSourcePrAsync(
        ProcurementDbContext dbContext,
        Guid purchaseRequestId,
        CancellationToken ct)
    {
        var pr = await dbContext.PurchaseRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == purchaseRequestId, ct)
            .ConfigureAwait(false);

        if (pr is null)
        {
            return (ProcurementCategory.Asset, []);
        }

        var lines = pr.LineItems
            .Select(li => new SourcePrLine(li.ItemDescription, li.StockNumber, li.CatalogItemId))
            .ToList();

        return (pr.Category, lines);
    }

    /// <summary>
    /// Maps command lines to domain line data, backfilling StockNumber/CatalogItemId from the matching PR line
    /// whenever the incoming line doesn't already carry them, then throws a 400 if a required reference is still
    /// missing — failing loudly at PO time rather than silently at IAR acceptance, where unresolved lines are
    /// skipped and never materialize downstream:
    /// <list type="bullet">
    /// <item><b>Supply</b> — every line must carry a <c>StockNumber</c> (matched to an Expendable product).</item>
    /// <item><b>Asset</b> — every line must carry a <c>CatalogItemId</c> (classified into the Asset Register).</item>
    /// </list>
    /// </summary>
    internal static List<PurchaseOrderLineItemData> ResolveAndValidate(
        IEnumerable<PurchaseOrderLineItemRequest> commandLines,
        ProcurementCategory category,
        IReadOnlyList<SourcePrLine> prLines)
    {
        var prLineByDescription = prLines
            .Where(l => !string.IsNullOrWhiteSpace(l.ItemDescription))
            .GroupBy(l => l.ItemDescription.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var lineItems = commandLines.Select(li =>
        {
            var stockNumber = li.StockNumber;
            var catalogItemId = li.CatalogItemId;

            if ((string.IsNullOrWhiteSpace(stockNumber) || catalogItemId is null)
                && !string.IsNullOrWhiteSpace(li.Description)
                && prLineByDescription.TryGetValue(li.Description.Trim(), out var prLine))
            {
                if (string.IsNullOrWhiteSpace(stockNumber))
                    stockNumber = prLine.StockNumber;
                catalogItemId ??= prLine.CatalogItemId;
            }

            return new PurchaseOrderLineItemData(stockNumber, li.Unit, li.Description, li.Quantity, li.UnitCost,
                catalogItemId);
        }).ToList();

        if (category == ProcurementCategory.Supply)
        {
            var unresolved = lineItems
                .Where(li => string.IsNullOrWhiteSpace(li.StockNumber))
                .Select(li => li.Description)
                .ToList();

            if (unresolved.Count > 0)
            {
                var items = string.Join(", ", unresolved);
                var message = $"Cannot save this Supply purchase order: the following line(s) have no Stock No, "
                    + $"so accepted stock could not be matched to an Expendable product: {items}. "
                    + "Ensure each item on the source purchase request references a screened product first.";
                throw new CustomException(message, Enumerable.Empty<string>(), HttpStatusCode.BadRequest);
            }
        }
        else // ProcurementCategory.Asset
        {
            var unresolved = lineItems
                .Where(li => li.CatalogItemId is null || li.CatalogItemId == Guid.Empty)
                .Select(li => li.Description)
                .ToList();

            if (unresolved.Count > 0)
            {
                var items = string.Join(", ", unresolved);
                var message = $"Cannot save this Asset purchase order: the following line(s) have no catalog item, "
                    + $"so accepted units could not be classified into the Asset Register: {items}. "
                    + "Ensure each item on the source purchase request references a catalog item first.";
                throw new CustomException(message, Enumerable.Empty<string>(), HttpStatusCode.BadRequest);
            }
        }

        return lineItems;
    }
}
