using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using AMIS.Modules.AssetRegister.Contracts.v1.Assets;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Assets.UpdateAssetImage;

public static class UpdateAssetImageEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}/image", Handle)
            .WithModuleName<UpdateAssetImageCommand>()
            .WithSummary("Set or clear an asset's photo")
            .Produces<AssetRegistryDto>()
            .RequirePermission(AssetRegisterPermissions.Assets.Update);

    private static async Task<IResult> Handle(
        Guid id, UpdateAssetImageCommand cmd, IMediator mediator, CancellationToken ct)
    {
        if (id != cmd.AssetRegistryId) return TypedResults.BadRequest("Route id and body id must match.");
        var result = await mediator.Send(cmd, ct);
        return TypedResults.Ok(result);
    }
}
