using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.AssetInspectionAcceptanceReports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.AssetIARs.AcceptAssetIAR;

public static class AcceptAssetIAREndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/accept", Handle)
            .WithName(nameof(AcceptAssetIARCommand))
            .WithSummary("Accept an asset IAR — triggers TangibleItem creation in AssetManagement")
            .Produces<AssetIARDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProcurementAcquisitionModuleConstants.Permissions.AssetIARs.Accept);

    private static async Task<IResult> Handle(Guid id, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AcceptAssetIARCommand(id), cancellationToken);
        return TypedResults.Ok(result);
    }
}
