using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetManagement.Features.v1.Locations;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetManagement.Features.v1.Locations.GetLocationById;

public static class GetLocationByIdEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(new GetLocationByIdQuery(id), ct)))
        .WithName(nameof(GetLocationByIdQuery))
        .WithSummary("Get a location by ID")
        .Produces<LocationDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .RequirePermission(AssetManagementModuleConstants.Permissions.Locations.View);
}

