using AMIS.Modules.AssetManagement.Data;
using AMIS.Modules.AssetManagement.Domain;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.MasterData.Contracts.v1.ReportSignatories;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetManagement.Features.v1.Reports.ReportOfSPIssued;

public sealed class GetRSPIQueryHandler(AssetManagementDbContext dbContext, IMediator mediator)
    : IQueryHandler<GetRSPIQuery, PagedRSPIResponse>
{
    public async ValueTask<PagedRSPIResponse> Handle(GetRSPIQuery query, CancellationToken cancellationToken)
    {
        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize   = query.PageSize   <= 0 ? 20 : query.PageSize;

        var q =
            from icsItem in dbContext.ICSItems
            join inv in dbContext.TangibleInventoryItems
                on icsItem.TangibleInventoryItemId equals inv.Id
            join catalogItem in dbContext.PropertyItemCatalog
                on inv.ItemId equals catalogItem.Id
            join ics in dbContext.InventoryCustodianSlips
                on icsItem.ICSId equals ics.Id
            select new { ics, icsItem, inv, catalogItem };

        if (query.ActiveOnly)
        {
            q = q.Where(x => x.ics.Status == ICSStatus.Active);
        }

        if (query.DateFrom.HasValue)
        {
            q = q.Where(x => x.ics.Date >= query.DateFrom.Value);
        }

        if (query.DateTo.HasValue)
        {
            q = q.Where(x => x.ics.Date <= query.DateTo.Value);
        }

        if (query.AssetType.HasValue)
        {
            q = q.Where(x => x.icsItem.AssetTypeAtTimeOfIssuance == query.AssetType.Value);
        }

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);
        var overallAmountTotal = await q
            .Select(x => x.icsItem.UnitCost)
            .SumAsync(cancellationToken)
            .ConfigureAwait(false);

        var rows = await q
            .OrderBy(x => x.ics.Date)
            .ThenBy(x => x.ics.ICSNo)
            .ThenBy(x => x.inv.PropertyNo)
            .ThenBy(x => x.inv.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.ics.Id,
                x.ics.ICSNo,
                x.ics.Date,
                ICSStatus = x.ics.Status.ToString(),
                x.ics.FundCluster,
                x.ics.ReceivedByEmployeeId,
                x.ics.IssuedFromEmployeeId,
                TangibleInventoryItemId = x.inv.Id,
                x.inv.PropertyNo,
                ItemCode = x.catalogItem.Code,
                ItemName = x.catalogItem.Name,
                AssetType = x.icsItem.AssetTypeAtTimeOfIssuance.ToString(),
                x.icsItem.UnitCost,
                x.ics.ExpiresOn
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var employeeIds = rows
            .Select(x => x.ReceivedByEmployeeId)
            .Concat(rows.Where(x => x.IssuedFromEmployeeId.HasValue).Select(x => x.IssuedFromEmployeeId!.Value))
            .Distinct()
            .ToList();

        var employeeMap = await mediator
            .Send(new GetEmployeeReferencesByIdsQuery(employeeIds), cancellationToken)
            .ConfigureAwait(false);

        var responseRows = rows
            .Select(x =>
            {
                employeeMap.TryGetValue(x.ReceivedByEmployeeId, out var receivedByEmployee);

                var issuedFromEmployee = x.IssuedFromEmployeeId.HasValue && employeeMap.TryGetValue(x.IssuedFromEmployeeId.Value, out var foundIssuedBy)
                    ? foundIssuedBy
                    : null;

                return new RSPIItemDto(
                    x.Id,
                    x.ICSNo,
                    x.Date,
                    x.ICSStatus,
                    x.FundCluster,
                    x.ReceivedByEmployeeId,
                    BuildEmployeeDisplayName(receivedByEmployee, x.ReceivedByEmployeeId),
                    receivedByEmployee?.PositionName,
                    receivedByEmployee?.OfficeName,
                    x.IssuedFromEmployeeId,
                    BuildEmployeeDisplayName(issuedFromEmployee),
                    issuedFromEmployee?.PositionName,
                    issuedFromEmployee?.OfficeName,
                    x.TangibleInventoryItemId,
                    x.PropertyNo,
                    x.ItemCode,
                    x.ItemName,
                    x.AssetType,
                    x.UnitCost,
                    x.ExpiresOn);
            })
            .ToList();

        var sections = rows
            .GroupBy(x => new { x.Id, x.ICSNo, x.Date, x.FundCluster, x.ICSStatus })
            .OrderBy(x => x.Key.Date)
            .ThenBy(x => x.Key.ICSNo)
            .Select(g => new RSPISectionDto(
                g.Key.Id,
                g.Key.ICSNo,
                g.Key.Date,
                g.Key.FundCluster,
                g.Key.ICSStatus,
                g.Count(),
                g.Sum(x => x.UnitCost)))
            .ToList();

        var signatories = await mediator
            .Send(new GetReportSignatoriesQuery("RSPI"), cancellationToken)
            .ConfigureAwait(false);

        var signatoryDtos = signatories
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .Select(x => new RSPISignatoryDto(
                x.SortOrder,
                x.Label,
                x.Name,
                x.Title))
            .ToList();

        return new PagedRSPIResponse(
            signatoryDtos,
            sections,
            responseRows,
            responseRows.Count,
            responseRows.Sum(x => x.UnitCost),
            pageNumber,
            pageSize,
            totalCount,
            overallAmountTotal);
    }

    private static string BuildEmployeeDisplayName(EmployeeReferenceDto? employee, Guid fallbackId)
    {
        if (employee is null)
        {
            return fallbackId.ToString();
        }

        return BuildEmployeeDisplayName(employee) ?? fallbackId.ToString();
    }

    private static string? BuildEmployeeDisplayName(EmployeeReferenceDto? employee)
    {
        if (employee is null)
        {
            return null;
        }

        var fullName = $"{employee.FirstName} {employee.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? null : fullName;
    }
}

