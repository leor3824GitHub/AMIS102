using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using AMIS.Modules.ProcurementPlanning.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.GetAnnualProcurementPlan;

public sealed class GetAnnualProcurementPlanQueryHandler(
    ProcurementPlanningDbContext dbContext) : IQueryHandler<GetAnnualProcurementPlanQuery, AnnualProcurementPlanDto>
{
    public async ValueTask<AnnualProcurementPlanDto> Handle(
        GetAnnualProcurementPlanQuery query, CancellationToken cancellationToken)
        => await AppReadProjection.BuildDtoAsync(dbContext, query.Id, cancellationToken).ConfigureAwait(false);
}

