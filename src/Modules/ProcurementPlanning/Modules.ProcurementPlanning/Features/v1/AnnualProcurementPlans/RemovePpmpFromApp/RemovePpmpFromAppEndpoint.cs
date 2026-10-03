using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementPlanning.Contracts.Permissions;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.RemovePpmpFromApp;

public static class RemovePpmpFromAppEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/ppmps/{ppmpId:guid}/remove", Handle)
            .WithName(nameof(RemovePpmpFromAppCommand))
            .WithSummary("Remove a consolidated PPMP from a Draft or Returned APP")
            .Produces<AnnualProcurementPlanDto>()
            .RequirePermission(ProcurementPlanningPermissions.AnnualProcurementPlans.Consolidate);

    private static async Task<IResult> Handle(Guid id, Guid ppmpId, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new RemovePpmpFromAppCommand(id, ppmpId), ct);
        return TypedResults.Ok(result);
    }
}
