using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.SearchInspectionAcceptanceReports;

public sealed class SearchInspectionAcceptanceReportsQueryHandler(
    ProcurementDbContext dbContext) : IQueryHandler<SearchInspectionAcceptanceReportsQuery, PagedResponse<InspectionAcceptanceReportSummaryDto>>
{
    public async ValueTask<PagedResponse<InspectionAcceptanceReportSummaryDto>> Handle(
        SearchInspectionAcceptanceReportsQuery query, CancellationToken cancellationToken)
    {
        var q = dbContext.InspectionAcceptanceReports.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            q = q.Where(x => EF.Functions.ILike(x.IarNumber, $"%{kw}%") || EF.Functions.ILike(x.SupplierName, $"%{kw}%"));
        }

        if (query.PurchaseOrderId.HasValue)
            q = q.Where(x => x.PurchaseOrderId == query.PurchaseOrderId.Value);

        if (query.Status.HasValue)
            q = q.Where(x => x.Status == query.Status.Value);

        if (query.FromDate.HasValue)
            q = q.Where(x => x.IarDate >= query.FromDate.Value);

        if (query.ToDate.HasValue)
            q = q.Where(x => x.IarDate <= query.ToDate.Value);

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var items = await q
            .OrderByDescending(x => x.CreatedOnUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var poIds = items.Select(x => x.PurchaseOrderId).Distinct().ToList();
        var poNumbers = await dbContext.PurchaseOrders
            .AsNoTracking()
            .Where(x => poIds.Contains(x.Id))
            .Select(x => new { x.Id, x.PoNumber })
            .ToDictionaryAsync(x => x.Id, x => x.PoNumber, cancellationToken)
            .ConfigureAwait(false);

        var dtos = items.Select(iar => new InspectionAcceptanceReportSummaryDto(
            iar.Id,
            iar.IarNumber,
            iar.IarDate,
            poNumbers.GetValueOrDefault(iar.PurchaseOrderId, string.Empty),
            iar.SupplierName,
            iar.LineItems.Count,
            iar.TotalAmount,
            iar.Status,
            iar.CreatedOnUtc,
            iar.InspectedById,
            iar.SignedCopy != null,
            iar.Category)).ToList();

        return new PagedResponse<InspectionAcceptanceReportSummaryDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
