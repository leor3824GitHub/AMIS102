using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.AssetProcurement.Contracts.v1.AssetInspectionAcceptanceReports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetProcurement.Features.v1.AssetIARs.UpdateAssetIAR;

public static class UpdateAssetIAREndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}", Handle)
            .WithName(nameof(UpdateAssetIARCommand))
            .WithSummary("Update a Draft asset IAR")
            .Produces<AssetIARDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetProcurementModuleConstants.Permissions.AssetIARs.Update);

    private static async Task<IResult> Handle(Guid id, UpdateAssetIARCommand command, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command with { Id = id }, cancellationToken);
        return TypedResults.Ok(result);
    }
}
