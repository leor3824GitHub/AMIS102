using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.AssetInspectionAcceptanceReports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.AssetIARs.UpdateAssetIAR;

public static class UpdateAssetIAREndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}", Handle)
            .WithName(nameof(UpdateAssetIARCommand))
            .WithSummary("Update a Draft asset IAR")
            .Produces<AssetIARDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProcurementAcquisitionModuleConstants.Permissions.AssetIARs.Update);

    private static async Task<IResult> Handle(Guid id, UpdateAssetIARCommand command, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command with { Id = id }, cancellationToken);
        return TypedResults.Ok(result);
    }
}
