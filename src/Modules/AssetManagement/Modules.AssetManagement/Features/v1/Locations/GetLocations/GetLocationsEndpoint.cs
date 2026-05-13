using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetManagement.Features.v1.Locations.GetLocations;

public static class GetLocationsEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", async (
            string? keyword,
            LocationType? type,
            Guid? parentLocationId,
            int pageNumber,
            int pageSize,
            IMediator mediator,
            CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(
                new GetLocationsQuery(keyword, type, parentLocationId, pageNumber, pageSize), ct)))
        .WithName(nameof(GetLocationsQuery))
        .WithSummary("Get paginated locations for asset accountability and placement")
        .RequirePermission(AssetManagementModuleConstants.Permissions.Locations.View);
}

