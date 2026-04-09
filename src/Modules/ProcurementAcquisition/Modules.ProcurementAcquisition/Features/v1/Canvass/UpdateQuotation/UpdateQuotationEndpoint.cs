using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.ProcurementAcquisition.Features.v1.Canvass.UpdateQuotation;

public static class UpdateQuotationEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/quotations/{quotationId:guid}", UpdateQuotation)
            .WithName(nameof(UpdateQuotationCommand))
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
