using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.AssetRegister.Contracts.v1.Assets;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetRegister.Features.v1.Assets.GetAssetByPropertyNo;

public static class GetAssetByPropertyNoEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/by-property-no/{propertyNo}", Handle)
            .WithName(nameof(GetAssetByPropertyNoQuery))
            .WithSummary("Look up an asset by COA 2020-006 PropertyNo")
            .Produces<AssetRegistryDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Assets.View);

    private static async Task<IResult> Handle(string propertyNo, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetAssetByPropertyNoQuery(propertyNo), ct);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }
}
