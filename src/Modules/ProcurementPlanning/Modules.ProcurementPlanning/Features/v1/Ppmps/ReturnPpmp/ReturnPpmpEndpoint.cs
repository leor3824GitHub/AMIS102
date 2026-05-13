using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.ReturnPpmp;

public static class ReturnPpmpEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/return", Handle)
            .WithName(nameof(ReturnPpmpCommand))
            .WithSummary("Return a submitted PPMP for revision")
            .Produces<PpmpDto>()
            .RequirePermission(ProcurementPlanningModuleConstants.Permissions.Ppmps.Return);

    private static async Task<IResult> Handle(
        Guid id, ReturnPpmpCommand command, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(command with { Id = id }, ct);
        return TypedResults.Ok(result);
    }
}

