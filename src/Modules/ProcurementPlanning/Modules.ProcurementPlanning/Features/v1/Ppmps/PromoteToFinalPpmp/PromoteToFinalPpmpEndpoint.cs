using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.PromoteToFinalPpmp;

public static class PromoteToFinalPpmpEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/promote-to-final", Handle)
            .WithName(nameof(PromoteToFinalPpmpCommand))
            .WithSummary("Promote an Approved Indicative PPMP to a new Final draft")
            .Produces<PpmpDto>(StatusCodes.Status201Created)
            .RequirePermission(ProcurementPlanningModuleConstants.Permissions.Ppmps.Amend);

    private static async Task<IResult> Handle(
        Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new PromoteToFinalPpmpCommand(id), ct);
        return TypedResults.Created($"/api/v1/procurement-planning/ppmps/{result.Id}", result);
    }
}
