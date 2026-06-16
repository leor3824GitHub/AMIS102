using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using AMIS.Modules.AssetRegister.Features.v1.Locations;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Locations.CreateLocation;

public static class CreateLocationEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", Handle)
            .WithModuleName<CreateLocationCommand>()
            .WithSummary("Create a location for asset placement and accountability")
            .Produces<LocationDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(AssetRegisterPermissions.Locations.Create);

    private static async Task<IResult> Handle(
        CreateLocationCommand command, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return TypedResults.Created($"/api/v1/asset-register/locations/{result.Id}", result);
    }
}
