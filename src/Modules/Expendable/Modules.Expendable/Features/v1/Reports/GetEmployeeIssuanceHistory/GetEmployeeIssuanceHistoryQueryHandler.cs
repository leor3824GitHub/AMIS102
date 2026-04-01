using FSH.Framework.Shared.Persistence;
using FSH.Modules.Expendable.Contracts.v1.Requests;
using FSH.Modules.Expendable.Data;
using FSH.Modules.Expendable.Domain.Requests;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Expendable.Features.v1.Reports.GetEmployeeIssuanceHistory;

public sealed class GetEmployeeIssuanceHistoryQueryHandler
    : IQueryHandler<GetEmployeeIssuanceHistoryQuery, PagedResponse<EmployeeIssuanceDto>>
{
    private readonly ExpendableDbContext _dbContext;

    public GetEmployeeIssuanceHistoryQueryHandler(ExpendableDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<PagedResponse<EmployeeIssuanceDto>> Handle(
        GetEmployeeIssuanceHistoryQuery query, CancellationToken cancellationToken)
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

        var totalCount = await requestsQuery.CountAsync(cancellationToken);

        var pageNumber = query.PageNumber ?? 1;
        var pageSize = query.PageSize ?? 10;

        var requests = await requestsQuery
            .OrderByDescending(r => r.LastModifiedOnUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        if (requests.Count == 0)
            return new PagedResponse<EmployeeIssuanceDto>
            {
                Items = [],
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

        // Load product details
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

        var issuances = requests.Select(r =>
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
                        product?.SKU ?? string.Empty,
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

        return new PagedResponse<EmployeeIssuanceDto>
        {
            Items = issuances,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
