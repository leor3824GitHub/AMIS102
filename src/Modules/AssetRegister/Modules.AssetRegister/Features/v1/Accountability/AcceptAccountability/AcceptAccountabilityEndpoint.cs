using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetRegister.Contracts.Permissions;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.AcceptAccountability;

public static class AcceptAccountabilityEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/mine/{id:guid}/accept", Handle)
            .WithModuleName<AcceptAccountabilityCommand>()
            .WithSummary("Accept a pending ICS/PAR issued to me (PendingAcceptance → Active)")
            .Produces<PropertyAccountabilityDto>()
            .Produces(StatusCodes.Status409Conflict)
            .RequirePermission(AssetRegisterPermissions.MyAccountability.Acknowledge);

    private static async Task<IResult> Handle(
        Guid id, AcceptAccountabilityCommand cmd, IMediator mediator, CancellationToken ct)
    {
        if (id != cmd.AccountabilityId) return TypedResults.BadRequest("Route id and body id must match.");
        var result = await mediator.Send(cmd, ct);
        return TypedResults.Ok(result);
    }
}
