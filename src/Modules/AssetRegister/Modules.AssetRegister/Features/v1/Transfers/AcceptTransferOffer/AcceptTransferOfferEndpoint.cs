using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using AMIS.Modules.AssetRegister.Contracts.v1.Transfers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Transfers.AcceptTransferOffer;

public static class AcceptTransferOfferEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}/accept", Handle)
            .WithModuleName<AcceptTransferOfferCommand>()
            .WithSummary("Accept an incoming transfer offer and link the PPERR posted against it")
            .Produces<AssetTransferOfferDto>()
            .RequirePermission(AssetRegisterPermissions.Transfers.Accept);

    private static async Task<IResult> Handle(
        Guid id, AcceptTransferOfferRequest request, IMediator mediator, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await mediator.Send(new AcceptTransferOfferCommand(id, request.ReceivingReportId), ct);
        return TypedResults.Ok(result);
    }

    public sealed record AcceptTransferOfferRequest(Guid ReceivingReportId);
}
