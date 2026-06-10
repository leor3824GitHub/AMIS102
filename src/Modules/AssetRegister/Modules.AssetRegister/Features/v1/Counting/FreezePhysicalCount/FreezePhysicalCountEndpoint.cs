using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.v1.Counting;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetRegister.Contracts.Permissions;

namespace AMIS.Modules.AssetRegister.Features.v1.Counting.FreezePhysicalCount;

public static class FreezePhysicalCountEndpoint
{
    public sealed record FreezePhysicalCountRequest(string OfficeOrderNo);

    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/freeze", Handle)
            .WithModuleName<FreezePhysicalCountCommand>()
            .WithSummary("Freeze the ledger for a draft count session - sets the Office Order No and blocks covered asset movements")
            .Produces<PhysicalCountSessionDto>()
            .RequirePermission(AssetRegisterPermissions.Count.Freeze);

    private static async Task<IResult> Handle(
        Guid id, FreezePhysicalCountRequest request, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new FreezePhysicalCountCommand(id, request.OfficeOrderNo), ct);
        return TypedResults.Ok(result);
    }
}
