using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.SubmitPpmp;

public static class SubmitPpmpEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/submit", Handle)
            .WithName(nameof(SubmitPpmpCommand))
            .WithSummary("Submit a PPMP for approval")
            .Produces<PpmpDto>()
            .RequirePermission(ProcurementPlanningModuleConstants.Permissions.Ppmps.Submit);

    private static async Task<IResult> Handle(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new SubmitPpmpCommand(id), ct);
        return TypedResults.Ok(result);
    }
}

