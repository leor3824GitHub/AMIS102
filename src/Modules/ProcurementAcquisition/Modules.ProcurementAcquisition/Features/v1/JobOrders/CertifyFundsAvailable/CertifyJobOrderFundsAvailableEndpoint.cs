using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.JobOrders;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.ProcurementAcquisition.Contracts.Permissions;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.CertifyFundsAvailable;

public static class CertifyJobOrderFundsAvailableEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/certify-funds-available", CertifyFundsAvailable)
            .WithName($"Procurement.{nameof(CertifyJobOrderFundsAvailableCommand)}")
            .WithSummary("Accountant signs the Funds Available portion of a job order and captures the BUR/ORS")
            .Produces<JobOrderDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProcurementPermissions.JobOrders.CertifyFundsAvailable);

    private static async Task<IResult> CertifyFundsAvailable(
        Guid id,
        CertifyJobOrderFundsAvailableCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command with { Id = id }, cancellationToken);
        return TypedResults.Ok(result);
    }
}
