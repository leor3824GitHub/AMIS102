using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetProcurement.Contracts.v1.AssetInspectionAcceptanceReports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetProcurement.Features.v1.AssetIARs.AssignPropertyNo;

public static class AssignPropertyNoEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/lines/{itemNo:int}/property-no", Handle)
            .WithName(nameof(AssignPropertyNoCommand))
            .WithSummary("Assign a Property No to a passed line during the Acceptance stage")
            .Produces<AssetIARDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetProcurementModuleConstants.Permissions.AssetIARs.AssignPropertyNo);

    private static async Task<IResult> Handle(Guid id, int itemNo, AssignPropertyNoCommand command, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command with { Id = id, ItemNo = itemNo }, cancellationToken);
        return TypedResults.Ok(result);
    }
}
