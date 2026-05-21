using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.AssetInspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.AssetIARs.SearchAcceptedIARLineItems;

/// <summary>
/// Flattens line items across Accepted IARs into a searchable list for the
/// Receiving Report "From IAR" autocomplete. Line items are stored in a JSON
/// column so filtering happens in-memory; the DB query is capped at 500 parent
/// IARs (most-recently-accepted first) to bound memory and latency.
/// </summary>
public sealed class SearchAcceptedIARLineItemsQueryHandler(
    ProcurementDbContext dbContext) : IQueryHandler<SearchAcceptedIARLineItemsQuery, PagedResponse<AcceptedIARLineItemDto>>
{
    public async ValueTask<PagedResponse<AcceptedIARLineItemDto>> Handle(
        SearchAcceptedIARLineItemsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var iars = await dbContext.AssetIARs
            .AsNoTracking()
            .Where(x => x.Status == AssetIARStatus.Accepted)
            .OrderByDescending(x => x.AcceptedOnUtc ?? x.CreatedOnUtc)
            .Take(500)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var poIds = iars.Select(x => x.PurchaseOrderId).Distinct().ToList();
        var supplierByPoId = await dbContext.PurchaseOrders
            .AsNoTracking()
            .Where(x => poIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => new { x.SupplierName, x.SupplierAddress }, cancellationToken)
            .ConfigureAwait(false);

        var keyword = query.Keyword?.Trim();
        var flat = iars.SelectMany(iar =>
        {
            supplierByPoId.TryGetValue(iar.PurchaseOrderId, out var po);
            return iar.LineItems
                .Where(li => li.InspectionResult != LineInspectionResult.Rejected
                          && !string.IsNullOrWhiteSpace(li.StockPropertyNo))
                .Select(li => new AcceptedIARLineItemDto(
                    iar.Id,
                    iar.IarNumber,
                    iar.IarDate,
                    li.ItemNo,
                    li.Description,
                    li.Unit,
                    li.Quantity,
                    li.UnitCost,
                    li.PropertyClassHint,
                    li.SerialNo,
                    li.Brand,
                    li.Model,
                    li.StockPropertyNo,
                    po?.SupplierName ?? iar.SupplierName,
                    po?.SupplierAddress));
        });

        if (!string.IsNullOrEmpty(keyword))
        {
            flat = flat.Where(x =>
                x.SupplierName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                x.StockPropertyNo!.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                x.IARNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                x.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                (x.PropertyClassHint != null && x.PropertyClassHint.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
        }

        var materialized = flat.ToList();
        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 20 : query.PageSize;

        var page = materialized
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResponse<AcceptedIARLineItemDto>
        {
            Items = page,
            TotalCount = materialized.Count,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
