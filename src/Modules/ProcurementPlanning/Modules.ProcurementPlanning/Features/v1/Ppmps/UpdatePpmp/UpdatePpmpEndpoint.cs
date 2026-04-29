using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.UpdatePpmp;

public static class UpdatePpmpEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}", Handle)
            .WithName(nameof(UpdatePpmpCommand))
            .WithSummary("Update a Draft PPMP")
            .Produces<PpmpDto>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProcurementPlanningModuleConstants.Permissions.Ppmps.Update);

    private static async Task<IResult> Handle(
        Guid id, UpdatePpmpCommand command, IMediator mediator, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(command with { Id = id }, ct);
            return TypedResults.Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return TypedResults.NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new { message = ex.Message });
        }
    }
}
