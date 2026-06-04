using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.ProcurementAcquisition.Contracts.Permissions;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.CreatePurchaseOrdersFromCanvass;

public static class CreatePurchaseOrdersFromCanvassEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/from-canvass", CreatePurchaseOrdersFromCanvass)
            .WithName($"Procurement.{nameof(CreatePurchaseOrdersFromCanvassCommand)}")
            .WithSummary("Generate one purchase order per winning supplier from an awarded canvass")
            .Produces<IReadOnlyList<PurchaseOrderDto>>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict)
            .RequirePermission(ProcurementPermissions.PurchaseOrders.Create);

    private static async Task<IResult> CreatePurchaseOrdersFromCanvass(
        CreatePurchaseOrdersFromCanvassCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return TypedResults.Created("/api/v1/procurement/purchase-orders", result);
    }
}
