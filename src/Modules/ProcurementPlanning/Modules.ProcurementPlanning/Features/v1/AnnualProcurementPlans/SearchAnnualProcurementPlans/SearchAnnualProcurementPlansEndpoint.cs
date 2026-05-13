using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.SearchAnnualProcurementPlans;

public static class SearchAnnualProcurementPlansEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", Handle)
            .WithName(nameof(SearchAnnualProcurementPlansQuery))
            .WithSummary("Search Annual Procurement Plans")
            .Produces<PagedResponse<AnnualProcurementPlanSummaryDto>>()
            .RequirePermission(ProcurementPlanningModuleConstants.Permissions.AnnualProcurementPlans.View);

    private static async Task<IResult> Handle(
        [AsParameters] SearchAnnualProcurementPlansQuery query, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return TypedResults.Ok(result);
    }
}

