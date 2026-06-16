using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using AMIS.Modules.AssetRegister.Features.v1.Locations;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Locations.GetLocationById;

public static class GetLocationByIdEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", Handle)
            .WithModuleName<GetLocationByIdQuery>()
            .WithSummary("Get a location by ID")
            .Produces<LocationDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetRegisterPermissions.Locations.View);

    private static async Task<IResult> Handle(Guid id, IMediator mediator, CancellationToken ct) =>
        TypedResults.Ok(await mediator.Send(new GetLocationByIdQuery(id), ct));
}
