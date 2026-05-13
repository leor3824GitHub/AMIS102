using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.MasterData.Contracts.v1.References;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.MasterData.Features.v1.UnitOfMeasures.DeleteUnitOfMeasure;

public static class DeleteUnitOfMeasureEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapDelete("/{id:guid}", DeleteUnitOfMeasure)
            .WithName(nameof(DeleteUnitOfMeasureCommand))
            .WithSummary("Delete unit of measure")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(MasterDataModuleConstants.Permissions.UnitOfMeasures.Delete);

    private static async Task<IResult> DeleteUnitOfMeasure(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteUnitOfMeasureCommand(id), cancellationToken);
        return TypedResults.NoContent();
    }
}
