using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementPlanning.Contracts.Permissions;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.DeletePpmp;

public static class DeletePpmpEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapDelete("/{id:guid}", Handle)
            .WithName(nameof(DeletePpmpCommand))
            .WithSummary("Delete a Draft PPMP")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission(ProcurementPlanningPermissions.Ppmps.Delete);

    private static async Task<IResult> Handle(Guid id, IMediator mediator, CancellationToken ct)
    {
        await mediator.Send(new DeletePpmpCommand(id), ct);
        return TypedResults.NoContent();
    }
}
