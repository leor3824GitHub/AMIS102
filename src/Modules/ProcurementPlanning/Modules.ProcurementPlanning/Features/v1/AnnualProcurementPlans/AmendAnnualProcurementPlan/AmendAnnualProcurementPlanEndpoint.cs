using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.AmendAnnualProcurementPlan;

public static class CreateUpdateAppEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/create-update", Handle)
            .WithName(nameof(CreateUpdateAppCommand))
            .WithSummary("Create a new Updated version of an Approved Final or Updated APP")
            .Produces<AnnualProcurementPlanDto>(StatusCodes.Status201Created)
            .RequirePermission(ProcurementPlanningModuleConstants.Permissions.AnnualProcurementPlans.Amend);

    private static async Task<IResult> Handle(
        Guid id, CreateUpdateAppCommand command, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(command with { Id = id }, ct);
        return TypedResults.Created($"/api/v1/procurement-planning/apps/{result.Id}", result);
    }
}
