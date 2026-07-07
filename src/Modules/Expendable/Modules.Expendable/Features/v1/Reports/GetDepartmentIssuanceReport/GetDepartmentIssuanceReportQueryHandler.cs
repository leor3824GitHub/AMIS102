using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Modules.Expendable.Data;
using AMIS.Modules.Expendable.Domain.Requests;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Reports.GetDepartmentIssuanceReport;

public sealed class GetDepartmentIssuanceReportQueryHandler
    : IQueryHandler<GetDepartmentIssuanceReportQuery, PagedResponse<DepartmentIssuanceSummaryDto>>
{
    private readonly ExpendableDbContext _dbContext;

    public GetDepartmentIssuanceReportQueryHandler(ExpendableDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<PagedResponse<DepartmentIssuanceSummaryDto>> Handle(
        GetDepartmentIssuanceReportQuery query, CancellationToken cancellationToken)
    {
        var pageNumber = query.PageNumber ?? 1;
        var pageSize = query.PageSize ?? 10;

        // Base query: fulfilled supply requests matching the filters.
        var requestsQuery = _dbContext.SupplyRequests
            .AsNoTracking()
            .Where(r => r.Status == SupplyRequestStatus.Fulfilled);

        if (!string.IsNullOrWhiteSpace(query.DepartmentId))
            requestsQuery = requestsQuery.Where(r => r.DepartmentId == query.DepartmentId);

        if (query.From.HasValue)
            requestsQuery = requestsQuery.Where(r => r.LastModifiedOnUtc >= query.From.Value);

        if (query.To.HasValue)
            requestsQuery = requestsQuery.Where(r => r.LastModifiedOnUtc <= query.To.Value);

        // The report groups (and pages) by department. Resolve the distinct department set and
        // paginate it in the DB (DepartmentId is an indexed scalar column) so we only materialize
        // the item-JSON for the departments on the current page — not every fulfilled request in the tenant.
        var departmentsQuery = requestsQuery.Select(r => r.DepartmentId).Distinct();

        var totalCount = await departmentsQuery.CountAsync(cancellationToken);

        var pagedDepartmentIds = await departmentsQuery
            .OrderBy(d => d)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        if (pagedDepartmentIds.Count == 0)
            return new PagedResponse<DepartmentIssuanceSummaryDto>
            {
                Items = [],
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

        // Load only the fulfilled requests for the departments on this page.
        var requests = await requestsQuery
            .Where(r => pagedDepartmentIds.Contains(r.DepartmentId))
            .ToListAsync(cancellationToken);

        // Load product details for all products referenced by the page's requests.
        var productIds = requests
            .SelectMany(r => r.Items)
            .Select(i => i.ProductId)
            .Distinct()
            .ToList();

        var products = await _dbContext.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.StockNo, p.UnitOfMeasure })
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        // Aggregate by department, preserving the paged department order.
        var byDepartment = requests
            .GroupBy(r => r.DepartmentId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var summaries = pagedDepartmentIds
            .Where(byDepartment.ContainsKey)
            .Select(deptId =>
            {
                var deptRequests = byDepartment[deptId];
                var productBreakdown = deptRequests
                    .SelectMany(r => r.Items.Where(i => i.FulfilledQuantity > 0))
                    .GroupBy(i => i.ProductId)
                    .Select(productGroup =>
                    {
                        var product = products.GetValueOrDefault(productGroup.Key);
                        var totalQty = productGroup.Sum(i => i.FulfilledQuantity);
                        var totalVal = productGroup.Sum(i => i.FulfilledValue);
                        return new DepartmentProductBreakdownDto(
                            productGroup.Key,
                            product?.Name ?? "Unknown",
                            product?.StockNo ?? string.Empty,
                            totalQty,
                            totalVal,
                            product?.UnitOfMeasure ?? string.Empty,
                            totalQty > 0 ? Math.Round(totalVal / totalQty, 4) : 0m
                        );
                    })
                    .OrderBy(p => p.ProductName)
                    .ToList();

                return new DepartmentIssuanceSummaryDto(
                    deptId,
                    deptRequests.Count,
                    productBreakdown.Sum(p => p.TotalQuantityIssued),
                    productBreakdown.Sum(p => p.TotalValue),
                    productBreakdown
                );
            })
            .ToList();

        return new PagedResponse<DepartmentIssuanceSummaryDto>
        {
            Items = summaries,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}

