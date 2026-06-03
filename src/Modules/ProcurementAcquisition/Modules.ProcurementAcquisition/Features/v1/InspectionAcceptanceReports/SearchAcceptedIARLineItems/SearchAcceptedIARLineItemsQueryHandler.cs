using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using AMIS.Modules.ProcurementAcquisition.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.SearchAcceptedIARLineItems;

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

        // Asset-only: this feeds AssetRegister's Receiving Report "From IAR" picker. Supply IARs now share
        // this table, so without the Category filter expendable lines would surface in asset receiving reports.
        // (Today they also lack a StockPropertyNo and would be dropped below, but that is incidental — filter
        // explicitly at the source so the guarantee doesn't depend on the property-number convention.)
        var iars = await dbContext.InspectionAcceptanceReports
            .AsNoTracking()
            .Where(x => x.Status == InspectionAcceptanceReportStatus.Accepted
                     && x.Category == ProcurementCategory.Asset)
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
                    po?.SupplierAddress,
                    li.CatalogItemId,
                    li.UacsObjectCode));
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
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

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
