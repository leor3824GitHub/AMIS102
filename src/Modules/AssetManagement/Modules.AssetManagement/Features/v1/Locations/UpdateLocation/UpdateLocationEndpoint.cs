using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetManagement.Features.v1.Locations;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetManagement.Features.v1.Locations.UpdateLocation;

public static class UpdateLocationEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}", async (Guid id, UpdateLocationCommand command, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(command with { Id = id }, ct);
                return TypedResults.Ok(result);
            })
        .WithName(nameof(UpdateLocationCommand))
        .WithSummary("Update an existing location")
        .Produces<LocationDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .RequirePermission(AssetManagementModuleConstants.Permissions.Locations.Update);
}

