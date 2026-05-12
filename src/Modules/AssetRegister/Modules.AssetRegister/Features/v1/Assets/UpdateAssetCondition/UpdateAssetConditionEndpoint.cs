using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.AssetRegister.Contracts.v1.Assets;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetRegister.Features.v1.Assets.UpdateAssetCondition;

public static class UpdateAssetConditionEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/condition", Handle)
            .WithName(nameof(UpdateAssetConditionCommand))
            .WithSummary("Update an asset's current physical condition")
            .Produces<AssetRegistryDto>()
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Assets.Update);

    private static async Task<IResult> Handle(
        Guid id, UpdateAssetConditionCommand cmd, IMediator mediator, CancellationToken ct)
    {
        if (id != cmd.AssetRegistryId) return TypedResults.BadRequest("Route id and body id must match.");
        var result = await mediator.Send(cmd, ct);
        return TypedResults.Ok(result);
    }
}
