using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Persistence;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.Expendable.Contracts.Permissions;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse.GetRejectedInventory;

public static class GetRejectedInventoryEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/rejected/search", Search)
            .WithName(nameof(GetRejectedInventoryQuery))
            .WithSummary("Get rejected inventory")
            .Produces<PagedResponse<RejectedInventoryDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(ExpendablePermissions.Inventory.ViewReports);

    private static async Task<IResult> Search(
        GetRejectedInventoryQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}


