using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MasterData.Features.v1.ModesOfProcurement.UpdateModeOfProcurement;

public static class UpdateModeOfProcurementEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id}", Update)
            .WithName(nameof(UpdateModeOfProcurementCommand))
            .WithSummary("Update mode of procurement")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(MasterDataModuleConstants.Permissions.ModesOfProcurement.Update);

    private static async Task<IResult> Update(
        Guid id,
        UpdateModeOfProcurementCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(command with { Id = id }, cancellationToken);
        return TypedResults.NoContent();
    }
}
