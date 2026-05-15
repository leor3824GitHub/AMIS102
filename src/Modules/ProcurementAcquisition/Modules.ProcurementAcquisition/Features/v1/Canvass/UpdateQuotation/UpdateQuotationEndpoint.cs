using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.UpdateQuotation;

public static class UpdateQuotationEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/quotations/{quotationId:guid}", UpdateQuotation)
            .WithName($"Procurement.{nameof(UpdateQuotationCommand)}")
            .WithSummary("Update a supplier quotation")
            .Produces<CanvassQuotationDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProcurementAcquisitionModuleConstants.Permissions.CanvassRequests.Update);

    private static async Task<IResult> UpdateQuotation(
        Guid quotationId,
        UpdateQuotationCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command with { QuotationId = quotationId }, cancellationToken);
        return TypedResults.Ok(result);
    }
}

