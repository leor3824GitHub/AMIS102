using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.AssetProcurement.Contracts.v1.AssetPurchaseRequests;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetProcurement.Features.v1.AssetPurchaseRequests.RejectAssetPurchaseRequest;

public static class RejectAssetPurchaseRequestEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/reject", Handle)
            .WithName(nameof(RejectAssetPurchaseRequestCommand))
            .WithSummary("Reject a submitted asset purchase request")
            .Produces<AssetPurchaseRequestDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetProcurementModuleConstants.Permissions.AssetPurchaseRequests.Reject);

    private static async Task<IResult> Handle(
        Guid id,
        RejectAssetPurchaseRequestCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command with { Id = id }, cancellationToken);
        return TypedResults.Ok(result);
    }
}
