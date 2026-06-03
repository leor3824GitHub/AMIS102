using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.ProcurementAcquisition.Contracts.Permissions;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.AssignPropertyNo;

public static class AssignPropertyNoEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/lines/{itemNo:int}/property-no", Handle)
            .WithName($"Procurement.{nameof(AssignPropertyNoCommand)}")
            .WithSummary("Assign a Property No to a passed line during the Acceptance stage")
            .Produces<InspectionAcceptanceReportDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProcurementPermissions.InspectionAcceptanceReports.AssignPropertyNo);

    private static async Task<IResult> Handle(Guid id, int itemNo, AssignPropertyNoCommand command, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command with { Id = id, ItemNo = itemNo }, cancellationToken);
        return TypedResults.Ok(result);
    }
}
