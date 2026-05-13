using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.MasterData.Features.v1.ModesOfProcurement.CreateModeOfProcurement;

public static class CreateModeOfProcurementEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", Create)
            .WithName(nameof(CreateModeOfProcurementCommand))
            .WithSummary("Create mode of procurement")
            .Produces<ModeOfProcurementDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(MasterDataModuleConstants.Permissions.ModesOfProcurement.Create);

    private static async Task<IResult> Create(
        CreateModeOfProcurementCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return TypedResults.Created($"/api/v1/master-data/modes-of-procurement/{result.Id}", result);
    }
}

