using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Expendable.Contracts.Permissions;
using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse.ListWarehouseLocations;

public static class ListWarehouseLocationsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/locations", ListLocations)
            .WithName("Expendable_ListWarehouseLocations")
            .WithSummary("List warehouse locations holding inventory (unpaged lookup)")
            .Produces<IReadOnlyList<WarehouseLocationDto>>(StatusCodes.Status200OK)
            .RequirePermission(ExpendablePermissions.Inventory.View);

    private static async Task<IResult> ListLocations(IMediator mediator, CancellationToken cancellationToken)
        => TypedResults.Ok(await mediator.Send(new ListWarehouseLocationsQuery(), cancellationToken));
}
