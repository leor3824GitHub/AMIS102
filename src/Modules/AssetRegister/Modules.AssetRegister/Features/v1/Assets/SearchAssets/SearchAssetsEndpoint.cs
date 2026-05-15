using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Assets;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Assets.SearchAssets;

public static class SearchAssetsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", Handle)
            .WithModuleName<SearchAssetsQuery>()
            .WithSummary("Search assets")
            .Produces<PagedResponse<AssetRegistrySummaryDto>>()
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Assets.View);

    private static async Task<IResult> Handle(
        IMediator mediator,
        string? keyword = null,
        AssetType? assetType = null,
        LifecycleState? lifecycleState = null,
        Guid? currentCustodianId = null,
        bool includeTransferredOut = false,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new SearchAssetsQuery(
            keyword, assetType, lifecycleState, currentCustodianId,
            includeTransferredOut, pageNumber, pageSize), ct);
        return TypedResults.Ok(result);
    }
}

