using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Assets;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetRegister.Contracts.Permissions;

namespace AMIS.Modules.AssetRegister.Features.v1.Assets.SearchAssets;

public static class SearchAssetsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", Handle)
            .WithModuleName<SearchAssetsQuery>()
            .WithSummary("Search assets")
            .Produces<PagedResponse<AssetRegistrySummaryDto>>()
            .RequirePermission(AssetRegisterPermissions.Assets.View);

    private static async Task<IResult> Handle(
        IMediator mediator,
        string? keyword = null,
        string? serialNo = null,
        AssetType? assetType = null,
        LifecycleState? lifecycleState = null,
        Guid? currentCustodianId = null,
        bool includeTransferredOut = false,
        int pageNumber = 1,
        int pageSize = 10,
        AssetCondition? currentCondition = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new SearchAssetsQuery(
            Keyword: keyword,
            SerialNo: serialNo,
            AssetType: assetType,
            LifecycleState: lifecycleState,
            CurrentCustodianId: currentCustodianId,
            IncludeTransferredOut: includeTransferredOut,
            PageNumber: pageNumber,
            PageSize: pageSize,
            CurrentCondition: currentCondition), ct);
        return TypedResults.Ok(result);
    }
}

