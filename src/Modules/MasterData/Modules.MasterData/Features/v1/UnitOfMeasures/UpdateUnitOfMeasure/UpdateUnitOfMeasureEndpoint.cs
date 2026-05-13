using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.MasterData.Contracts.v1.References;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.MasterData.Features.v1.UnitOfMeasures.UpdateUnitOfMeasure;

public static class UpdateUnitOfMeasureEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}", UpdateUnitOfMeasure)
            .WithName(nameof(UpdateUnitOfMeasureCommand))
            .WithSummary("Update unit of measure")
            .Produces<UnitOfMeasureReferenceDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(MasterDataModuleConstants.Permissions.UnitOfMeasures.Update);

    private static async Task<IResult> UpdateUnitOfMeasure(
        Guid id,
        UpdateUnitOfMeasureCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var cmd = command with { Id = id };
        var result = await mediator.Send(cmd, cancellationToken);
        return TypedResults.Ok(result);
    }
}
