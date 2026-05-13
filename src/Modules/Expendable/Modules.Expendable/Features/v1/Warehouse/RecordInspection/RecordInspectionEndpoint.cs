using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse.RecordInspection;

public static class RecordInspectionEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/purchases/{purchaseId:guid}/inspect", RecordInspection)
            .WithName(nameof(RecordInspectionCommand))
            .WithSummary("Record purchase inspection")
            .Produces<RecordInspectionResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ExpendableModuleConstants.Permissions.Inventory.Receive);

    private static async Task<IResult> RecordInspection(
        Guid purchaseId,
        RecordInspectionCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var cmd = command with { PurchaseId = purchaseId };
        var result = await mediator.Send(cmd, cancellationToken);
        return TypedResults.Ok(result);
    }
}


