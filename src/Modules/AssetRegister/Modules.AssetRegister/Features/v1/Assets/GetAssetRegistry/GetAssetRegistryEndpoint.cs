using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.v1.Assets;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetRegister.Contracts.Permissions;

namespace AMIS.Modules.AssetRegister.Features.v1.Assets.GetAssetRegistry;

public static class GetAssetRegistryEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", Handle)
            .WithModuleName<GetAssetRegistryQuery>()
            .WithSummary("Get an asset by id")
            .Produces<AssetRegistryDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetRegisterPermissions.Assets.View);

    private static async Task<IResult> Handle(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetAssetRegistryQuery(id), ct);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }
}

