using AMIS.Framework.Persistence;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Modules.Expendable.Data;
using AMIS.Modules.Expendable.Domain.Requests;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Requests.SearchSupplyRequests;

public sealed class SearchSupplyRequestsQueryHandler : IQueryHandler<SearchSupplyRequestsQuery, PagedResponse<SupplyRequestDto>>
{
    private readonly ExpendableDbContext _dbContext;

    public SearchSupplyRequestsQueryHandler(ExpendableDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<PagedResponse<SupplyRequestDto>> Handle(SearchSupplyRequestsQuery query, CancellationToken cancellationToken)
    {
        var requestQuery = _dbContext.SupplyRequests.AsNoTracking();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<SupplyRequestStatus>(query.Status, out var status))
        {
            requestQuery = requestQuery.Where(r => r.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.EmployeeId))
        {
            requestQuery = requestQuery.Where(r => r.EmployeeId == query.EmployeeId);
        }

        if (!string.IsNullOrWhiteSpace(query.DepartmentId))
        {
            requestQuery = requestQuery.Where(r => r.DepartmentId == query.DepartmentId);
        }

        requestQuery = requestQuery.OrderByDescending(r => r.RequestDate);

        var projected = requestQuery.Select(r => r.ToSupplyRequestDto());
        return await projected.ToPagedResponseAsync(query, cancellationToken).ConfigureAwait(false);
    }
}

