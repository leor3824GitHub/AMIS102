using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetRegister.Contracts.Permissions;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.CancelAccountability;

public static class CancelAccountabilityEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/cancel", Handle)
            .WithModuleName<CancelAccountabilityCommand>()
            .WithSummary("Cancel an Active accountability that has no returned/lost lines")
            .Produces<PropertyAccountabilityDto>()
            .RequirePermission(AssetRegisterPermissions.Accountability.Cancel);

    private static async Task<IResult> Handle(
        Guid id, CancelAccountabilityCommand cmd, IMediator mediator, CancellationToken ct)
    {
        if (id != cmd.AccountabilityId) return TypedResults.BadRequest("Route id and body id must match.");
        var result = await mediator.Send(cmd, ct);
        return TypedResults.Ok(result);
    }
}

