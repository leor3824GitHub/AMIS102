using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.CreateUpdateApp;

public static class CreateUpdateAppEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/create-update", Handle)
            .WithName(nameof(CreateUpdateAppCommand))
            .WithSummary("Create a new Updated version of an Approved Final or Updated APP")
            .Produces<AnnualProcurementPlanDto>(StatusCodes.Status201Created)
            .RequirePermission(ProcurementPlanningModuleConstants.Permissions.AnnualProcurementPlans.CreateUpdate);

    private static async Task<IResult> Handle(
        Guid id, CreateUpdateAppCommand command, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(command with { Id = id }, ct);
        return TypedResults.Created($"/api/v1/procurement-planning/apps/{result.Id}", result);
    }
}

