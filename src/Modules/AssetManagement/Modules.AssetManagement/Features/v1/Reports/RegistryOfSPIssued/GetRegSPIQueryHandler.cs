using FSH.Modules.AssetManagement.Data;
using FSH.Modules.MasterData.Contracts.v1.References;
using FSH.Modules.MasterData.Contracts.v1.ReportSignatories;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.Reports.RegistryOfSPIssued;

public sealed class GetRegSPIQueryHandler(AssetManagementDbContext dbContext, IMediator mediator)
    : IQueryHandler<GetRegSPIQuery, PagedRegSPIResponse>
{
    public async ValueTask<PagedRegSPIResponse> Handle(GetRegSPIQuery query, CancellationToken cancellationToken)
    {
        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize   = query.PageSize   <= 0 ? 20 : query.PageSize;

        // Base: ICS items for this employee, joined to inventory item and catalog.
        var q =
            from icsItem in dbContext.ICSItems
            join inv in dbContext.TangibleInventoryItems
                on icsItem.TangibleInventoryItemId equals inv.Id
            join catalogItem in dbContext.PropertyItemCatalog
                on inv.ItemId equals catalogItem.Id
            join ics in dbContext.InventoryCustodianSlips
                on icsItem.ICSId equals ics.Id
            where ics.ReceivedByEmployeeId == query.EmployeeId
            select new { ics, icsItem, inv, catalogItem };

        if (query.AssetType.HasValue)
        {
            q = q.Where(x => x.icsItem.AssetTypeAtTimeOfIssuance == query.AssetType.Value);
        }

        if (query.Status.HasValue)
        {
            q = q.Where(x => x.ics.Status == query.Status.Value);
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
                x.ics.FundCluster,
                TangibleInventoryItemId = x.inv.Id,
                x.inv.PropertyNo,
                ItemCode = x.catalogItem.Code,
                ItemName = x.catalogItem.Name,
                AssetType = x.icsItem.AssetTypeAtTimeOfIssuance.ToString(),
                x.icsItem.UnitCost,
                x.icsItem.EstimatedUsefulLifeYears,
                x.ics.ExpiresOn,
                ICSStatus = x.ics.Status.ToString(),
                x.ics.IssuedFromEmployeeId
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var employeeIds = rows
            .Where(x => x.IssuedFromEmployeeId.HasValue)
            .Select(x => x.IssuedFromEmployeeId!.Value)
            .Append(query.EmployeeId)
            .Distinct()
            .ToList();

        var employeeMap = await mediator
            .Send(new GetEmployeeReferencesByIdsQuery(employeeIds), cancellationToken)
            .ConfigureAwait(false);

        employeeMap.TryGetValue(query.EmployeeId, out var requestedEmployee);

        var signatories = await mediator
            .Send(new GetReportSignatoriesQuery("RegSPI"), cancellationToken)
            .ConfigureAwait(false);

        var signatoryDtos = signatories
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .Select(x => new RegSPISignatoryDto(
                x.SortOrder,
                x.Label,
                x.Name,
                x.Title))
            .ToList();

        var responseRows = rows
            .Select(x =>
            {
                var issuedFrom = x.IssuedFromEmployeeId.HasValue && employeeMap.TryGetValue(x.IssuedFromEmployeeId.Value, out var found)
                    ? found
                    : null;

                return new RegSPIEntryDto(
                    x.Id,
                    x.ICSNo,
                    x.Date,
                    x.FundCluster,
                    x.TangibleInventoryItemId,
                    x.PropertyNo,
                    x.ItemCode,
                    x.ItemName,
                    x.AssetType,
                    x.UnitCost,
                    x.EstimatedUsefulLifeYears,
                    x.ExpiresOn,
                    x.ICSStatus,
                    x.IssuedFromEmployeeId,
                    BuildEmployeeDisplayName(issuedFrom),
                    issuedFrom?.PositionName,
                    issuedFrom?.OfficeName);
            })
            .ToList();

        var sections = rows
            .GroupBy(x => new { x.Id, x.ICSNo, x.Date, x.FundCluster, x.ICSStatus })
            .OrderBy(x => x.Key.Date)
            .ThenBy(x => x.Key.ICSNo)
            .Select(g => new RegSPISectionDto(
                g.Key.Id,
                g.Key.ICSNo,
                g.Key.Date,
                g.Key.FundCluster,
                g.Key.ICSStatus,
                g.Count(),
                g.Sum(x => x.UnitCost)))
            .ToList();

        return new PagedRegSPIResponse(
            query.EmployeeId,
            requestedEmployee?.EmployeeNumber,
            BuildEmployeeDisplayName(requestedEmployee, query.EmployeeId),
            requestedEmployee?.OfficeName,
            requestedEmployee?.DepartmentName,
            requestedEmployee?.PositionName,
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
