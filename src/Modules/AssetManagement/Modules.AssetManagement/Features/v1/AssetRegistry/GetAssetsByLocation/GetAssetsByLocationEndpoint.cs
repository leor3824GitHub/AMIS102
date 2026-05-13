using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetManagement.Features.v1.AssetRegistryQueries.GetAssetsByLocation;

public static class GetAssetsByLocationEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/by-location/{locationId:guid}", async (
            Guid locationId,
            string? keyword,
            int pageNumber,
            int pageSize,
            IMediator mediator,
            CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(
                new GetAssetsByLocationQuery(locationId, keyword, pageNumber, pageSize), ct)))
        .WithName(nameof(GetAssetsByLocationQuery))
        .WithSummary("Get paginated assets by current location from the unified asset registry")
        .RequirePermission(AssetManagementModuleConstants.Permissions.AssetRegistry.View);
}

