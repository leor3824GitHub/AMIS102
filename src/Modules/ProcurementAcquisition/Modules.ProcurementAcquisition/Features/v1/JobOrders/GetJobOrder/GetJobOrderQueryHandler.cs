using AMIS.Modules.ProcurementAcquisition.Contracts.v1.JobOrders;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.CreateJobOrder;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.GetJobOrder;

public sealed class GetJobOrderQueryHandler(ProcurementDbContext dbContext)
    : IQueryHandler<GetJobOrderQuery, JobOrderDto?>
{
    public async ValueTask<JobOrderDto?> Handle(GetJobOrderQuery query, CancellationToken cancellationToken)
    {
        var jo = await dbContext.JobOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);

        return jo is null ? null : CreateJobOrderCommandHandler.MapToDto(jo);
    }
}
