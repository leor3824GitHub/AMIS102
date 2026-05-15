using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.AssetInspectionAcceptanceReports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.AssetIARs.CreateAssetIAR;

public static class CreateAssetIAREndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", Handle)
            .WithName($"Procurement.{nameof(CreateAssetIARCommand)}")
            .WithSummary("Create an asset inspection and acceptance report")
            .Produces<AssetIARDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(ProcurementAcquisitionModuleConstants.Permissions.AssetIARs.Create);

    private static async Task<IResult> Handle(
        CreateAssetIARCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return TypedResults.Created($"/api/v1/procurement/iars/{result.Id}", result);
    }
}
