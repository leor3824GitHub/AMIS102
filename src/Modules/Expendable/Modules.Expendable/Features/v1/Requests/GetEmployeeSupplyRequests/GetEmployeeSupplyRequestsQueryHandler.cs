using AMIS.Framework.Persistence;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Modules.Expendable.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Requests.GetEmployeeSupplyRequests;

public sealed class GetEmployeeSupplyRequestsQueryHandler : IQueryHandler<GetEmployeeSupplyRequestsQuery, PagedResponse<SupplyRequestDto>>
{
    private readonly ExpendableDbContext _dbContext;

    public GetEmployeeSupplyRequestsQueryHandler(ExpendableDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<PagedResponse<SupplyRequestDto>> Handle(GetEmployeeSupplyRequestsQuery query, CancellationToken cancellationToken)
    {
        var requestQuery = _dbContext.SupplyRequests.AsNoTracking()
            .Where(r => r.EmployeeId == query.EmployeeId)
            .OrderByDescending(r => r.RequestDate);

        var projected = requestQuery.Select(r => r.ToSupplyRequestDto());
        return await projected.ToPagedResponseAsync(query, cancellationToken).ConfigureAwait(false);
    }
}

