using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.RenewAccountability;

public static class RenewAccountabilityEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/renew", Handle)
            .WithName(nameof(RenewAccountabilityCommand))
            .WithSummary("Renew an Active accountability — produces a successor row")
            .Produces<PropertyAccountabilityDto>(StatusCodes.Status201Created)
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Accountability.Issue);

    private static async Task<IResult> Handle(
        Guid id, RenewAccountabilityCommand cmd, IMediator mediator, CancellationToken ct)
    {
        if (id != cmd.AccountabilityId) return TypedResults.BadRequest("Route id and body id must match.");
        var result = await mediator.Send(cmd, ct);
        return TypedResults.Created($"/api/v1/asset-register/accountability/{result.Id}", result);
    }
}

