using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.CancelAccountability;

public static class CancelAccountabilityEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/cancel", Handle)
            .WithName(nameof(CancelAccountabilityCommand))
            .WithSummary("Cancel an Active accountability that has no returned/lost lines")
            .Produces<PropertyAccountabilityDto>()
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Accountability.Issue);

    private static async Task<IResult> Handle(
        Guid id, CancelAccountabilityCommand cmd, IMediator mediator, CancellationToken ct)
    {
        if (id != cmd.AccountabilityId) return TypedResults.BadRequest("Route id and body id must match.");
        var result = await mediator.Send(cmd, ct);
        return TypedResults.Ok(result);
    }
}

