using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetManagement.Features.v1.AssetRegistryQueries.GetAssetsByCustodian;

public static class GetAssetsByCustodianEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/by-custodian/{custodianId:guid}", async (
            Guid custodianId,
            string? keyword,
            int pageNumber,
            int pageSize,
            IMediator mediator,
            CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(
                new GetAssetsByCustodianQuery(custodianId, keyword, pageNumber, pageSize), ct)))
        .WithName(nameof(GetAssetsByCustodianQuery))
        .WithSummary("Get paginated assets by current custodian from the unified asset registry")
        .RequirePermission(AssetManagementModuleConstants.Permissions.AssetRegistry.View);
}

