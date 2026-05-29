using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementAcquisition.Contracts.Permissions;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.GetCanvassablePrLines;

public static class GetCanvassablePrLinesEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/pr/{purchaseRequestId:guid}/canvassable-lines", GetCanvassablePrLines)
            .WithName("Procurement_GetCanvassablePrLines")
            .WithSummary("List a purchase request's line items with canvass-coverage status")
            .Produces<IReadOnlyList<CanvassablePrLineDto>>(StatusCodes.Status200OK)
            .RequirePermission(ProcurementPermissions.CanvassRequests.Create);

    private static async Task<IResult> GetCanvassablePrLines(
        Guid purchaseRequestId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCanvassablePrLinesQuery(purchaseRequestId), cancellationToken);
        return TypedResults.Ok(result);
    }
}
