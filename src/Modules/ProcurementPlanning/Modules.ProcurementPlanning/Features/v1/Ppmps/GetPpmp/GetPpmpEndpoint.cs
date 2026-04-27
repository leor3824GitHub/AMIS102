using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.GetPpmp;

public static class GetPpmpEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", Handle)
            .WithName(nameof(GetPpmpQuery))
            .WithSummary("Get a PPMP by ID")
            .Produces<PpmpDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProcurementPlanningModuleConstants.Permissions.Ppmps.View);

    private static async Task<IResult> Handle(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetPpmpQuery(id), ct);
        return TypedResults.Ok(result);
    }
}
