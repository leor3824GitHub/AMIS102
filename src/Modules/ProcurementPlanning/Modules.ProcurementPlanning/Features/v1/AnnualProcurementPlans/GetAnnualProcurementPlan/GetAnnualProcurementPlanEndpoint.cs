using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.GetAnnualProcurementPlan;

public static class GetAnnualProcurementPlanEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", Handle)
            .WithName(nameof(GetAnnualProcurementPlanQuery))
            .WithSummary("Get an APP by ID")
            .Produces<AnnualProcurementPlanDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProcurementPlanningModuleConstants.Permissions.AnnualProcurementPlans.View);

    private static async Task<IResult> Handle(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetAnnualProcurementPlanQuery(id), ct);
        return TypedResults.Ok(result);
    }
}
