using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.CertifyFundsAvailable;

public static class CertifyFundsAvailableEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/certify-funds-available", CertifyFundsAvailable)
            .WithName($"Procurement.{nameof(CertifyFundsAvailableCommand)}")
            .WithSummary("Accountant signs the Funds Available portion of a PR and assigns UACS Object Codes per line")
            .Produces<PurchaseRequestDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProcurementAcquisitionModuleConstants.Permissions.PurchaseRequests.CertifyFundsAvailable);

    private static async Task<IResult> CertifyFundsAvailable(
        Guid id,
        CertifyFundsAvailableCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command with { Id = id }, cancellationToken);
        return TypedResults.Ok(result);
    }
}
