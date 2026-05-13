using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.MasterData.Contracts.v1.References;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.MasterData.Features.v1.UnitOfMeasures.CreateUnitOfMeasure;

public static class CreateUnitOfMeasureEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", CreateUnitOfMeasure)
            .WithName(nameof(CreateUnitOfMeasureCommand))
            .WithSummary("Create unit of measure")
            .Produces<UnitOfMeasureReferenceDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(MasterDataModuleConstants.Permissions.UnitOfMeasures.Create);

    private static async Task<IResult> CreateUnitOfMeasure(
        CreateUnitOfMeasureCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return TypedResults.Created($"/api/v1/master-data/unit-of-measures/{result.Id}", result);
    }
}
