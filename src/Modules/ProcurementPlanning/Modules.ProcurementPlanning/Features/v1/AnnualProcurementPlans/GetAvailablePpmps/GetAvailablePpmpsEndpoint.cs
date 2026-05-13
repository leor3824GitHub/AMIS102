using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.GetAvailablePpmps;

public static class GetAvailablePpmpsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/available-ppmps", Handle)
            .WithName(nameof(GetAvailablePpmpsForAppQuery))
            .WithSummary("Get approved PPMPs available for consolidation into an APP")
            .Produces<IReadOnlyList<PpmpSummaryDto>>()
            .RequirePermission(ProcurementPlanningModuleConstants.Permissions.AnnualProcurementPlans.Consolidate);

    private static async Task<IResult> Handle(
        [AsParameters] GetAvailablePpmpsForAppQuery query, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return TypedResults.Ok(result);
    }
}

