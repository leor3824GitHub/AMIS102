using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Expendable.Contracts.Permissions;
using AMIS.Modules.Expendable.Contracts.v1.Products;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Products.GetProductsByIds;

public static class GetProductsByIdsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/by-ids", GetByIds)
            .WithName("Expendable_GetProductsByIds")
            .WithSummary("Get products for a set of ids (batch)")
            .Produces<IReadOnlyList<ProductDto>>(StatusCodes.Status200OK)
            .RequirePermission(ExpendablePermissions.Products.View);

    private static async Task<IResult> GetByIds(
        [FromBody] IReadOnlyCollection<Guid> ids,
        IMediator mediator,
        CancellationToken cancellationToken)
        => TypedResults.Ok(await mediator.Send(new GetProductsByIdsQuery(ids ?? []), cancellationToken));
}
