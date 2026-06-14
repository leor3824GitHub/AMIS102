using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using AMIS.Modules.AssetRegister.Contracts.v1.Assets;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Assets.UpdateAssetDepreciation;

public static class UpdateAssetDepreciationEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/depreciation", Handle)
            .WithModuleName<UpdateAssetDepreciationCommand>()
            .WithSummary("Override an asset's depreciation parameters (residual value, useful life, method)")
            .Produces<AssetRegistryDto>()
            .RequirePermission(AssetRegisterPermissions.Assets.Update);

    private static async Task<IResult> Handle(
        Guid id, UpdateAssetDepreciationCommand cmd, IMediator mediator, CancellationToken ct)
    {
        if (id != cmd.AssetRegistryId) return TypedResults.BadRequest("Route id and body id must match.");
        var result = await mediator.Send(cmd, ct);
        return TypedResults.Ok(result);
    }
}
