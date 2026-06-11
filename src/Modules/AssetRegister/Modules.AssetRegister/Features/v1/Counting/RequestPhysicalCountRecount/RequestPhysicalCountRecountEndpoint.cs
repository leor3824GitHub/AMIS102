using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.v1.Counting;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetRegister.Contracts.Permissions;

namespace AMIS.Modules.AssetRegister.Features.v1.Counting.RequestPhysicalCountRecount;

public static class RequestPhysicalCountRecountEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/entries/{entryId:guid}/recount", Handle)
            .WithModuleName<RequestPhysicalCountRecountCommand>()
            .WithSummary("Flag a recorded entry for recount during reconciliation")
            .Produces<PhysicalCountSessionDto>()
            .RequirePermission(AssetRegisterPermissions.Count.Submit);

    private static async Task<IResult> Handle(
        Guid id, Guid entryId, RecountReasonRequest? body, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(
            new RequestPhysicalCountRecountCommand(id, entryId, body?.Reason), ct);
        return TypedResults.Ok(result);
    }
}

/// <summary>Optional body carrying the recount reason; session and entry come from the route.</summary>
public sealed record RecountReasonRequest(string? Reason);
