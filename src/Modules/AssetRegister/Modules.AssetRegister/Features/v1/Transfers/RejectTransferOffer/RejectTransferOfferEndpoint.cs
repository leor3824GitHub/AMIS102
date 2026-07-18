using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using AMIS.Modules.AssetRegister.Contracts.v1.Transfers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Transfers.RejectTransferOffer;

public static class RejectTransferOfferEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}/reject", Handle)
            .WithModuleName<RejectTransferOfferCommand>()
            .WithSummary("Reject an incoming transfer offer")
            .Produces<AssetTransferOfferDto>()
            .RequirePermission(AssetRegisterPermissions.Transfers.Reject);

    private static async Task<IResult> Handle(
        Guid id, RejectTransferOfferRequest request, IMediator mediator, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await mediator.Send(new RejectTransferOfferCommand(id, request.Reason), ct);
        return TypedResults.Ok(result);
    }

    public sealed record RejectTransferOfferRequest(string Reason);
}
