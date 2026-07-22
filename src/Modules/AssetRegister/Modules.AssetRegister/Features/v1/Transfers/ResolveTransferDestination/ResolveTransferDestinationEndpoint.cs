using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using AMIS.Modules.AssetRegister.Contracts.v1.Transfers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Transfers.ResolveTransferDestination;

public static class ResolveTransferDestinationEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/destination-for-employee/{employeeId:guid}", Handle)
            .WithModuleName<ResolveTransferDestinationQuery>()
            .WithSummary("Resolve the agency a recipient employee belongs to")
            .Produces<TransferDestinationDto>()
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission(AssetRegisterPermissions.Transfers.Offer);

    private static async Task<IResult> Handle(Guid employeeId, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new ResolveTransferDestinationQuery(employeeId), ct);

        // No linked destination is the ordinary outcome for an internal or off-AMIS recipient, so it is a
        // 204 rather than a 404 — the caller simply falls back to the manual handshake.
        return result is null ? TypedResults.NoContent() : TypedResults.Ok(result);
    }
}
