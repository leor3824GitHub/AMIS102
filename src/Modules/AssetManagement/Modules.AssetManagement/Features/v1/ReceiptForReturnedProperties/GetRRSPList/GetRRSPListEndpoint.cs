using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.ReceiptForReturnedProperties.GetRRSPList;

public static class GetRRSPListEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", GetRRSPList)
            .WithName(nameof(GetRRSPListQuery))
            .WithSummary("Get a paginated list of Receipts for Returned Semi-Expendable Properties")
            .Produces<PagedRRSPListResponse>(StatusCodes.Status200OK)
            .RequirePermission(AssetManagementModuleConstants.Permissions.ReceiptForReturnedProperties.View);

    private static async Task<IResult> GetRRSPList(
        [AsParameters] GetRRSPListQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}
