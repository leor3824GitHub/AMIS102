using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetProcurement.Contracts.v1.AssetInspectionAcceptanceReports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetProcurement.Features.v1.AssetIARs.CancelAssetIAR;

public static class CancelAssetIAREndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/cancel", Handle)
            .WithName(nameof(CancelAssetIARCommand))
            .WithSummary("Cancel an IAR before it is accepted")
            .Produces<AssetIARDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetProcurementModuleConstants.Permissions.AssetIARs.Cancel);

    private static async Task<IResult> Handle(Guid id, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CancelAssetIARCommand(id), cancellationToken);
        return TypedResults.Ok(result);
    }
}
