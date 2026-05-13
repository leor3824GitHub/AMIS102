using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.CreateUpdatePpmp;

public static class CreateUpdatePpmpEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/create-update", Handle)
            .WithName(nameof(CreateUpdatePpmpCommand))
            .WithSummary("Create a new Updated version of an Approved/Consolidated Final or Updated PPMP")
            .Produces<PpmpDto>(StatusCodes.Status201Created)
            .RequirePermission(ProcurementPlanningModuleConstants.Permissions.Ppmps.CreateUpdate);

    private static async Task<IResult> Handle(
        Guid id, CreateUpdatePpmpCommand command, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(command with { Id = id }, ct);
        return TypedResults.Created($"/api/v1/procurement-planning/ppmps/{result.Id}", result);
    }
}

