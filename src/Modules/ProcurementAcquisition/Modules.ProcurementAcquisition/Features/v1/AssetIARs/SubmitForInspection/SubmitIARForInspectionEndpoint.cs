using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.AssetInspectionAcceptanceReports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.AssetIARs.SubmitForInspection;

public static class SubmitIARForInspectionEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/submit-for-inspection", Handle)
            .WithName($"Procurement.{nameof(SubmitIARForInspectionCommand)}")
            .WithSummary("Submit a Draft IAR to the assigned inspector")
            .Produces<AssetIARDto>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProcurementAcquisitionModuleConstants.Permissions.AssetIARs.SubmitForInspection);

    private static async Task<IResult> Handle(Guid id, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SubmitIARForInspectionCommand(id), cancellationToken);
        return TypedResults.Ok(result);
    }
}
