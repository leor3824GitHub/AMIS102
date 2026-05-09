using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.AssetManagement.Features.v1.Locations;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.Locations.CreateLocation;

public static class CreateLocationEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", async (CreateLocationCommand command, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(command, ct);
                return TypedResults.Created($"/api/v1/asset-management/locations/{result.Id}", result);
            })
        .WithName(nameof(CreateLocationCommand))
        .WithSummary("Create a location for asset placement and accountability")
        .Produces<LocationDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .RequirePermission(AssetManagementModuleConstants.Permissions.Locations.Create);
}
