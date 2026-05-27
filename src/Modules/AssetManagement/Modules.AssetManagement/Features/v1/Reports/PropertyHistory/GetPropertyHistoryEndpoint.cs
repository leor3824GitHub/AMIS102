using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetManagement.Contracts.Permissions;

namespace AMIS.Modules.AssetManagement.Features.v1.Reports.PropertyHistory;

public static class GetPropertyHistoryEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/property-history/{propertyId:guid}", GetHistory)
            .WithName(nameof(GetPropertyHistoryQuery))
            .WithSummary("Property accountability history â€” full lifecycle audit trail for a single property unit")
            .Produces<PropertyHistoryDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetManagementPermissions.Reports.View);

    private static async Task<IResult> GetHistory(
        Guid propertyId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPropertyHistoryQuery(propertyId), cancellationToken);
        return TypedResults.Ok(result);
    }
}

