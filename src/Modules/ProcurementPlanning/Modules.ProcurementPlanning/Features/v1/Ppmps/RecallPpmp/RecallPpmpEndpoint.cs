using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.RecallPpmp;

public static class RecallPpmpEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/recall", Handle)
            .WithName(nameof(RecallPpmpCommand))
            .WithSummary("Recall a submitted PPMP back to draft")
            .Produces<PpmpDto>()
            .RequirePermission(ProcurementPlanningModuleConstants.Permissions.Ppmps.Submit);

    private static async Task<IResult> Handle(
        Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new RecallPpmpCommand(id), ct);
        return TypedResults.Ok(result);
    }
}
