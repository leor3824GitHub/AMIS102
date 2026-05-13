using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetManagement.Features.v1.Reports.SemiExpendablePropertyCard;

public static class GetSPCEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/spc/{itemId:guid}", GetSPC)
            .WithName(nameof(GetSPCQuery))
            .WithSummary("Semi-expendable Property Card (SPC) — stock movement history for a catalog item type")
            .Produces<SPCDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetManagementModuleConstants.Permissions.Reports.View);

    private static async Task<IResult> GetSPC(
        Guid itemId,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetSPCQuery(itemId, dateFrom, dateTo), cancellationToken);
        return TypedResults.Ok(result);
    }
}

