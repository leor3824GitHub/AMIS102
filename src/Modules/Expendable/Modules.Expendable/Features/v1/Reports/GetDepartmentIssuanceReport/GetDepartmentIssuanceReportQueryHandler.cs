using FSH.Framework.Shared.Persistence;
using FSH.Modules.Expendable.Contracts.v1.Requests;
using FSH.Modules.Expendable.Data;
using FSH.Modules.Expendable.Domain.Requests;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Expendable.Features.v1.Reports.GetDepartmentIssuanceReport;

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
        // Load fulfilled supply requests with their items
        var requestsQuery = _dbContext.SupplyRequests
            .AsNoTracking()
            .Where(r => r.Status == SupplyRequestStatus.Fulfilled);

        if (!string.IsNullOrWhiteSpace(query.DepartmentId))
            requestsQuery = requestsQuery.Where(r => r.DepartmentId == query.DepartmentId);

        if (query.From.HasValue)
            requestsQuery = requestsQuery.Where(r => r.LastModifiedOnUtc >= query.From.Value);

        if (query.To.HasValue)
            requestsQuery = requestsQuery.Where(r => r.LastModifiedOnUtc <= query.To.Value);

        var requests = await requestsQuery.ToListAsync(cancellationToken);

        if (requests.Count == 0)
            return new PagedResponse<DepartmentIssuanceSummaryDto>
            {
                Items = [],
                TotalCount = 0,
                PageNumber = query.PageNumber ?? 1,
                PageSize = query.PageSize ?? 10
            };

        // Load product details for all products referenced by the requests
        var productIds = requests
            .SelectMany(r => r.Items)
            .Select(i => i.ProductId)
            .Distinct()
            .ToList();

        var products = await _dbContext.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.SKU })
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        // Aggregate by department
        var grouped = requests
            .GroupBy(r => r.DepartmentId)
            .OrderBy(g => g.Key)
            .ToList();

        var pageNumber = query.PageNumber ?? 1;
        var pageSize = query.PageSize ?? 10;
        var totalCount = grouped.Count;

        var pagedGroups = grouped
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var summaries = pagedGroups.Select(deptGroup =>
        {
            var productBreakdown = deptGroup
                .SelectMany(r => r.Items.Where(i => i.FulfilledQuantity > 0))
                .GroupBy(i => i.ProductId)
                .Select(productGroup =>
                {
                    var product = products.GetValueOrDefault(productGroup.Key);
                    return new DepartmentProductBreakdownDto(
                        productGroup.Key,
                        product?.Name ?? "Unknown",
                        product?.SKU ?? string.Empty,
                        productGroup.Sum(i => i.FulfilledQuantity),
                        productGroup.Sum(i => i.FulfilledValue)
                    );
                })
                .OrderBy(p => p.ProductName)
                .ToList();

            return new DepartmentIssuanceSummaryDto(
                deptGroup.Key,
                deptGroup.Count(),
                productBreakdown.Sum(p => p.TotalQuantityIssued),
                productBreakdown.Sum(p => p.TotalValue),
                productBreakdown
            );
        }).ToList();

        return new PagedResponse<DepartmentIssuanceSummaryDto>
        {
            Items = summaries,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
