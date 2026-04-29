using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.PromoteToFinalApp;

public static class PromoteToFinalAppEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/promote-to-final", Handle)
            .WithName(nameof(PromoteToFinalAppCommand))
            .WithSummary("Promote an Approved Indicative APP to a new Final draft")
            .Produces<AnnualProcurementPlanDto>(StatusCodes.Status201Created)
            .RequirePermission(ProcurementPlanningModuleConstants.Permissions.AnnualProcurementPlans.Amend);

    private static async Task<IResult> Handle(
        Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new PromoteToFinalAppCommand(id), ct);
        return TypedResults.Created($"/api/v1/procurement-planning/apps/{result.Id}", result);
    }
}
