using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.v1.Counting;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Counting.ClosePhysicalCount;

public static class ClosePhysicalCountEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/close", Handle)
            .WithModuleName<ClosePhysicalCountCommand>()
            .WithSummary("Close a reconciled count session")
            .Produces<PhysicalCountSessionDto>()
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Count.Close);

    private static async Task<IResult> Handle(
        Guid id, ClosePhysicalCountCommand cmd, IMediator mediator, CancellationToken ct)
    {
        if (id != cmd.SessionId) return TypedResults.BadRequest("Route id and body id must match.");
        var result = await mediator.Send(cmd, ct);
        return TypedResults.Ok(result);
    }
}

