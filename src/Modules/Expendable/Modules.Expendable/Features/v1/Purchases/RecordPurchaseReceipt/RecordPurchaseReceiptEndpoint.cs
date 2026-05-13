using AMIS.Modules.Expendable.Contracts.v1.Purchases;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Purchases.RecordPurchaseReceipt;

public static class RecordPurchaseReceiptEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{purchaseId:guid}/receipt", RecordReceipt)
            .WithName(nameof(RecordPurchaseReceiptCommand))
            .WithSummary("Record purchase receipt")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ExpendableModuleConstants.Permissions.Purchases.Receive);

    private static async Task<IResult> RecordReceipt(
        Guid purchaseId,
        RecordPurchaseReceiptCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var cmd = command with { PurchaseId = purchaseId };
        await mediator.Send(cmd, cancellationToken);
        return TypedResults.NoContent();
    }
}


