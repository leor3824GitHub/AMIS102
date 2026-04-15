using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.ReceiptForReturnedProperties.CreateRRSP;

public static class CreateRRSPEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", CreateRRSP)
            .WithName(nameof(CreateRRSPCommand))
            .WithSummary("Create a Receipt for Returned Semi-Expendable Property (RRSP) — cancels an Active ICS and returns all items to supply")
            .Produces<CreateRRSPResult>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetManagementModuleConstants.Permissions.ReceiptForReturnedProperties.Create);

    private static async Task<IResult> CreateRRSP(
        CreateRRSPCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return TypedResults.Created($"/api/v1/asset-management/receipt-for-returned-properties/{result.RRSPId}", result);
    }
}
