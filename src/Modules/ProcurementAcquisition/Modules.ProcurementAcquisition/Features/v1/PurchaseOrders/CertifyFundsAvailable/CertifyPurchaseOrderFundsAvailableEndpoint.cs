using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.ProcurementAcquisition.Contracts.Permissions;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.CertifyFundsAvailable;

public static class CertifyPurchaseOrderFundsAvailableEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/certify-funds-available", CertifyFundsAvailable)
            .WithName($"Procurement.{nameof(CertifyPurchaseOrderFundsAvailableCommand)}")
            .WithSummary("Accountant signs the Funds Available portion of a PO and assigns UACS Object Codes per line")
            .Produces<PurchaseOrderDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProcurementPermissions.PurchaseOrders.CertifyFundsAvailable);

    private static async Task<IResult> CertifyFundsAvailable(
        Guid id,
        CertifyPurchaseOrderFundsAvailableCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command with { Id = id }, cancellationToken);
        return TypedResults.Ok(result);
    }
}
