using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.v1.Counting;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Counting.MarkPhysicalCountMissing;

public static class MarkPhysicalCountMissingEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/missing", Handle)
            .WithName(nameof(MarkPhysicalCountMissingCommand))
            .WithSummary("Mark a known asset as missing during a count")
            .Produces<PhysicalCountSessionDto>()
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Count.Record);

    private static async Task<IResult> Handle(
        Guid id, MarkPhysicalCountMissingCommand cmd, IMediator mediator, CancellationToken ct)
    {
        if (id != cmd.SessionId) return TypedResults.BadRequest("Route id and body id must match.");
        var result = await mediator.Send(cmd, ct);
        return TypedResults.Ok(result);
    }
}

