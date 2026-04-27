using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using FSH.Modules.ProcurementPlanning.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.GetAnnualProcurementPlan;

public sealed class GetAnnualProcurementPlanQueryHandler(
    ProcurementPlanningDbContext dbContext) : IQueryHandler<GetAnnualProcurementPlanQuery, AnnualProcurementPlanDto>
{
    public async ValueTask<AnnualProcurementPlanDto> Handle(
        GetAnnualProcurementPlanQuery query, CancellationToken cancellationToken)
    {
        var app = await dbContext.AnnualProcurementPlans
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"APP {query.Id} not found.");

        return AppMapper.ToDto(app);
    }
}
