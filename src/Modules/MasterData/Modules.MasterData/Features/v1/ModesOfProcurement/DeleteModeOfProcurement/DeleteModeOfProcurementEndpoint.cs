using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MasterData.Features.v1.ModesOfProcurement.DeleteModeOfProcurement;

public static class DeleteModeOfProcurementEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapDelete("/{id}", Delete)
            .WithName(nameof(DeleteModeOfProcurementCommand))
            .WithSummary("Delete mode of procurement")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(MasterDataModuleConstants.Permissions.ModesOfProcurement.Delete);

    private static async Task<IResult> Delete(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteModeOfProcurementCommand(id), cancellationToken);
        return TypedResults.NoContent();
    }
}
