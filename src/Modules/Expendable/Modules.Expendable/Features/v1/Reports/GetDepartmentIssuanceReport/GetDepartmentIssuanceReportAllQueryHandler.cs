using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Modules.Expendable.Data;
using AMIS.Modules.Expendable.Domain.Requests;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Reports.GetDepartmentIssuanceReport;

// Full-dataset (unpaged) department issuance report. Same aggregation as the paged handler
// but returns every department in one response, so the report page fetches once instead of
// looping pages against the 100-row page-size clamp.
public sealed class GetDepartmentIssuanceReportAllQueryHandler
    : IQueryHandler<GetDepartmentIssuanceReportAllQuery, IReadOnlyList<DepartmentIssuanceSummaryDto>>
{
    private readonly ExpendableDbContext _dbContext;

    public GetDepartmentIssuanceReportAllQueryHandler(ExpendableDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<IReadOnlyList<DepartmentIssuanceSummaryDto>> Handle(
        GetDepartmentIssuanceReportAllQuery query, CancellationToken cancellationToken)
    {
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
            return [];

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

        return requests
            .GroupBy(r => r.DepartmentId)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var deptRequests = g.ToList();
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
                    g.Key,
                    deptRequests.Count,
                    productBreakdown.Sum(p => p.TotalQuantityIssued),
                    productBreakdown.Sum(p => p.TotalValue),
                    productBreakdown
                );
            })
            .ToList();
    }
}
