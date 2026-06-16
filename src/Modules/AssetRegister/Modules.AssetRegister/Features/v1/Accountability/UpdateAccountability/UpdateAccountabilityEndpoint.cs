using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetRegister.Contracts.Permissions;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.UpdateAccountability;

public static class UpdateAccountabilityEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}", Handle)
            .WithModuleName<UpdateAccountabilityCommand>()
            .WithSummary("Edit a still-pending ICS/PAR (header + lines) before acceptance")
            .Produces<PropertyAccountabilityDto>()
            .RequirePermission(AssetRegisterPermissions.Accountability.Update);

    private static async Task<IResult> Handle(
        Guid id, UpdateAccountabilityCommand cmd, IMediator mediator, CancellationToken ct)
    {
        if (id != cmd.AccountabilityId) return TypedResults.BadRequest("Route id and body id must match.");
        var result = await mediator.Send(cmd, ct);
        return TypedResults.Ok(result);
    }
}
