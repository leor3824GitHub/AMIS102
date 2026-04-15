using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.ReceiptForReturnedProperties.GetRRSPById;

public static class GetRRSPByIdEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", GetRRSPById)
            .WithName(nameof(GetRRSPByIdQuery))
            .WithSummary("Get a Receipt for Returned Semi-Expendable Property by ID")
            .Produces<RRSPDetailsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetManagementModuleConstants.Permissions.ReceiptForReturnedProperties.View);

    private static async Task<IResult> GetRRSPById(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetRRSPByIdQuery(id), cancellationToken);
        return TypedResults.Ok(result);
    }
}
