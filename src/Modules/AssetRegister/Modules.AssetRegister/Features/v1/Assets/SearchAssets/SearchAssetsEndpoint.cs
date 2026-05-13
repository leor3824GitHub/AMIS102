using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.AssetRegister.Contracts.v1;
using FSH.Modules.AssetRegister.Contracts.v1.Assets;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetRegister.Features.v1.Assets.SearchAssets;

public static class SearchAssetsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", Handle)
            .WithName(nameof(SearchAssetsQuery))
            .WithSummary("Search assets")
            .Produces<PagedResponse<AssetRegistrySummaryDto>>()
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Assets.View);

    private static async Task<IResult> Handle(
        IMediator mediator,
        string? keyword = null,
        AssetType? assetType = null,
        LifecycleState? lifecycleState = null,
        Guid? currentCustodianId = null,
        Guid? catalogItemId = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new SearchAssetsQuery(
            keyword, assetType, lifecycleState, currentCustodianId, catalogItemId, pageNumber, pageSize), ct);
        return TypedResults.Ok(result);
    }
}
