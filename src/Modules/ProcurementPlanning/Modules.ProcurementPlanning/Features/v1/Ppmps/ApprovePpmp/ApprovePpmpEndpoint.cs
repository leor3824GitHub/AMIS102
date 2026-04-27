using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.ApprovePpmp;

public static class ApprovePpmpEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/approve", Handle)
            .WithName(nameof(ApprovePpmpCommand))
            .WithSummary("Approve a submitted PPMP")
            .Produces<PpmpDto>()
            .RequirePermission(ProcurementPlanningModuleConstants.Permissions.Ppmps.Approve);

    private static async Task<IResult> Handle(
        Guid id, ApprovePpmpCommand command, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(command with { Id = id }, ct);
        return TypedResults.Ok(result);
    }
}
