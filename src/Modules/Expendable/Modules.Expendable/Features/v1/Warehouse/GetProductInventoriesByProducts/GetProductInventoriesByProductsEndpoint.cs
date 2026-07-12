using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Expendable.Contracts.Permissions;
using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse.GetProductInventoriesByProducts;

public static class GetProductInventoriesByProductsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/inventory/by-products", GetByProducts)
            .WithName("Expendable_GetProductInventoriesByProducts")
            .WithSummary("Get inventory rows for a set of products (batch)")
            .Produces<IReadOnlyList<ProductInventoryDto>>(StatusCodes.Status200OK)
            .RequirePermission(ExpendablePermissions.Inventory.View);

    private static async Task<IResult> GetByProducts(
        [FromBody] IReadOnlyCollection<Guid> productIds,
        IMediator mediator,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await mediator.Send(new GetProductInventoriesByProductsQuery(productIds ?? []), cancellationToken));
}
