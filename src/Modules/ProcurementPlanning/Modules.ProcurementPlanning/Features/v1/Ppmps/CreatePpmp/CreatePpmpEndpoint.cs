using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.CreatePpmp;

public static class CreatePpmpEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", Handle)
            .WithName(nameof(CreatePpmpCommand))
            .WithSummary("Create a new PPMP")
            .Produces<PpmpDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(ProcurementPlanningModuleConstants.Permissions.Ppmps.Create);

    private static async Task<IResult> Handle(
        CreatePpmpCommand command, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return TypedResults.Created($"/api/v1/procurement-planning/ppmps/{result.Id}", result);
    }
}
