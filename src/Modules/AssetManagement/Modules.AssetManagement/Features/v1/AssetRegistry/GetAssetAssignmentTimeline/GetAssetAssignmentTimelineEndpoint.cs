using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetManagement.Contracts.Permissions;

namespace AMIS.Modules.AssetManagement.Features.v1.AssetRegistryQueries.GetAssetAssignmentTimeline;

public static class GetAssetAssignmentTimelineEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{assetRegistryId:guid}/assignment-history", async (
            Guid assetRegistryId,
            int pageNumber,
            int pageSize,
            IMediator mediator,
            CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(
                new GetAssetAssignmentTimelineQuery(assetRegistryId, pageNumber, pageSize), ct)))
        .WithName(nameof(GetAssetAssignmentTimelineQuery))
        .WithSummary("Get assignment and status timeline for a specific asset registry record")
        .RequirePermission(AssetManagementPermissions.AssetRegistry.View);
}

