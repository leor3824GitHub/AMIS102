using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.AssetProcurement.Contracts.v1.AssetInspectionAcceptanceReports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetProcurement.Features.v1.AssetIARs.GetAssetIAR;

public static class GetAssetIAREndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", Handle)
            .WithName(nameof(GetAssetIARQuery))
            .WithSummary("Get asset IAR by ID")
            .Produces<AssetIARDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetProcurementModuleConstants.Permissions.AssetIARs.View);

    private static async Task<IResult> Handle(Guid id, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAssetIARQuery(id), cancellationToken);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }
}
