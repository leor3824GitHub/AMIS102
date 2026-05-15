using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetProcurement.Contracts.v1.AssetInspectionAcceptanceReports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetProcurement.Features.v1.AssetIARs.ReassignInspector;

public static class ReassignInspectorEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/reassign-inspector", Handle)
            .WithName(nameof(ReassignInspectorCommand))
            .WithSummary("Reassign the inspector while the IAR is awaiting inspection")
            .Produces<AssetIARDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetProcurementModuleConstants.Permissions.AssetIARs.SubmitForInspection);

    private static async Task<IResult> Handle(Guid id, ReassignInspectorCommand command, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command with { Id = id }, cancellationToken);
        return TypedResults.Ok(result);
    }
}
