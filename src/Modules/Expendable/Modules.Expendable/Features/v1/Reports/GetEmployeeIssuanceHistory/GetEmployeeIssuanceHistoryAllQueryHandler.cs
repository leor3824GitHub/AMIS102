using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Modules.Expendable.Data;
using AMIS.Modules.Expendable.Domain.Requests;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Reports.GetEmployeeIssuanceHistory;

// Full-dataset (unpaged) employee issuance history — returns every fulfilled request in one
// response so the report page fetches once instead of looping pages.
public sealed class GetEmployeeIssuanceHistoryAllQueryHandler
    : IQueryHandler<GetEmployeeIssuanceHistoryAllQuery, IReadOnlyList<EmployeeIssuanceDto>>
{
    private readonly ExpendableDbContext _dbContext;

    public GetEmployeeIssuanceHistoryAllQueryHandler(ExpendableDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<IReadOnlyList<EmployeeIssuanceDto>> Handle(
        GetEmployeeIssuanceHistoryAllQuery query, CancellationToken cancellationToken)
    {
        var requestsQuery = _dbContext.SupplyRequests
            .AsNoTracking()
            .Where(r => r.Status == SupplyRequestStatus.Fulfilled);

        if (!string.IsNullOrWhiteSpace(query.EmployeeId))
            requestsQuery = requestsQuery.Where(r => r.EmployeeId == query.EmployeeId);

        if (query.From.HasValue)
            requestsQuery = requestsQuery.Where(r => r.LastModifiedOnUtc >= query.From.Value);

        if (query.To.HasValue)
            requestsQuery = requestsQuery.Where(r => r.LastModifiedOnUtc <= query.To.Value);

        var requests = await requestsQuery
            .OrderByDescending(r => r.LastModifiedOnUtc)
            .ToListAsync(cancellationToken);

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
            .Select(p => new { p.Id, p.Name, p.StockNo })
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        return requests.Select(r =>
        {
            var items = r.Items
                .Where(i => i.FulfilledQuantity > 0)
                .Select(i =>
                {
                    var product = products.GetValueOrDefault(i.ProductId);
                    var unitPrice = i.FulfilledQuantity > 0
                        ? Math.Round(i.FulfilledValue / i.FulfilledQuantity, 4)
                        : 0m;
                    return new IssuanceItemDto(
                        i.ProductId,
                        product?.Name ?? "Unknown",
                        product?.StockNo ?? string.Empty,
                        i.FulfilledQuantity,
                        unitPrice,
                        i.FulfilledValue
                    );
                })
                .ToList();

            return new EmployeeIssuanceDto(
                r.Id,
                r.RequestNumber,
                r.EmployeeId,
                r.DepartmentId,
                r.LastModifiedOnUtc ?? r.CreatedOnUtc,
                items,
                items.Sum(i => i.TotalValue)
            );
        }).ToList();
    }
}
