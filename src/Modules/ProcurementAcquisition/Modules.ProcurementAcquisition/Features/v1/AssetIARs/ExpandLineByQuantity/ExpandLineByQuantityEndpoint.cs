using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.AssetInspectionAcceptanceReports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.AssetIARs.ExpandLineByQuantity;

public static class ExpandLineByQuantityEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/lines/{itemNo:int}/expand", Handle)
            .WithName(nameof(ExpandLineByQuantityCommand))
            .WithSummary("Split a passed line with Qty>1 into N lines of Qty=1 (NFA one-line-per-physical-unit)")
            .Produces<AssetIARDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProcurementAcquisitionModuleConstants.Permissions.AssetIARs.AssignPropertyNo);

    private static async Task<IResult> Handle(Guid id, int itemNo, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ExpandLineByQuantityCommand(id, itemNo), cancellationToken);
        return TypedResults.Ok(result);
    }
}
