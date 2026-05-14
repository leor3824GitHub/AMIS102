using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.v1.Assets;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Assets.RegisterAsset;

public static class RegisterAssetEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", Handle)
            .WithModuleName<RegisterAssetCommand>()
            .WithSummary("Register a single physical asset (one row per unit; operator-assigned PropertyNo).")
            .Produces<AssetRegistryDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Assets.Register);

    private static async Task<IResult> Handle(RegisterAssetCommand cmd, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(cmd, ct);
        return TypedResults.Created($"/api/v1/asset-register/assets/{result.Id}", result);
    }
}

