using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementPlanning.Contracts.Permissions;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.ReturnPpmpFromApp;

public static class ReturnPpmpFromAppEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/ppmps/{ppmpId:guid}/return", Handle)
            .WithName(nameof(ReturnPpmpFromAppCommand))
            .WithSummary("Remove a consolidated PPMP from a Draft or Returned APP and return it to its end-user for revision")
            .Produces<AnnualProcurementPlanDto>()
            .RequirePermission(ProcurementPlanningPermissions.AnnualProcurementPlans.Consolidate);

    private static async Task<IResult> Handle(
        Guid id, Guid ppmpId, ReturnPpmpFromAppCommand command, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(command with { AppId = id, PpmpId = ppmpId }, ct);
        return TypedResults.Ok(result);
    }
}
