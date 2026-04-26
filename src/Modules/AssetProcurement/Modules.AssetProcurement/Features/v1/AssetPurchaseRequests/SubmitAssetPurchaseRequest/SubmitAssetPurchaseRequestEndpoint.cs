using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.AssetProcurement.Contracts.v1.AssetPurchaseRequests;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetProcurement.Features.v1.AssetPurchaseRequests.SubmitAssetPurchaseRequest;

public static class SubmitAssetPurchaseRequestEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/submit", Handle)
            .WithName(nameof(SubmitAssetPurchaseRequestCommand))
            .WithSummary("Submit asset purchase request for approval")
            .Produces<AssetPurchaseRequestDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetProcurementModuleConstants.Permissions.AssetPurchaseRequests.Submit);

    private static async Task<IResult> Handle(Guid id, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SubmitAssetPurchaseRequestCommand(id), cancellationToken);
        return TypedResults.Ok(result);
    }
}
