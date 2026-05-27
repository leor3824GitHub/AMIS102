using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.v1.Assets;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetRegister.Contracts.Permissions;

namespace AMIS.Modules.AssetRegister.Features.v1.Assets.GetAssetByPropertyNo;

public static class GetAssetByPropertyNoEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/by-property-no/{propertyNo}", Handle)
            .WithModuleName<GetAssetByPropertyNoQuery>()
            .WithSummary("Look up an asset by COA 2020-006 PropertyNo")
            .Produces<AssetRegistryDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetRegisterPermissions.Assets.View);

    private static async Task<IResult> Handle(string propertyNo, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetAssetByPropertyNoQuery(propertyNo), ct);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }
}

