using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Assets;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetRegister.Contracts.Permissions;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.GetMyAccountableAssets;

public static class GetMyAccountableAssetsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/mine/assets", Handle)
            .WithModuleName<GetMyAccountableAssetsQuery>()
            .WithSummary("Get the individual assets currently issued to the current employee (per-asset view of My Accountability)")
            .Produces<PagedResponse<AssetRegistrySummaryDto>>()
            .RequirePermission(AssetRegisterPermissions.MyAccountability.View);

    private static async Task<IResult> Handle(
        IMediator mediator,
        string? keyword = null,
        AssetType? assetType = null,
        LifecycleState? lifecycleState = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetMyAccountableAssetsQuery(
            keyword, assetType, lifecycleState, pageNumber, pageSize), ct);
        return TypedResults.Ok(result);
    }
}
