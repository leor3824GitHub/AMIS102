using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementAcquisition.Contracts.Permissions;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.GetAwardedPrLines;

public static class GetAwardedPrLinesEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/pr/{purchaseRequestId:guid}/awarded-lines", GetAwardedPrLines)
            .WithName("Procurement_GetAwardedPrLines")
            .WithSummary("List a purchase request's line items already awarded across its canvasses")
            .Produces<IReadOnlyList<AwardedPrLineDto>>(StatusCodes.Status200OK)
            .RequirePermission(ProcurementPermissions.CanvassRequests.Award);

    private static async Task<IResult> GetAwardedPrLines(
        Guid purchaseRequestId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAwardedPrLinesQuery(purchaseRequestId), cancellationToken);
        return TypedResults.Ok(result);
    }
}
