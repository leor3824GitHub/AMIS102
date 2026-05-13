using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Modules.Expendable.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Requests.GetSupplyRequest;

public sealed class GetSupplyRequestQueryHandler : IQueryHandler<GetSupplyRequestQuery, SupplyRequestDto?>
{
    private readonly ExpendableDbContext _dbContext;

    public GetSupplyRequestQueryHandler(ExpendableDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<SupplyRequestDto?> Handle(GetSupplyRequestQuery query, CancellationToken cancellationToken)
    {
        var request = await _dbContext.SupplyRequests
            .FirstOrDefaultAsync(r => r.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);
        return request?.ToSupplyRequestDto();
    }
}

