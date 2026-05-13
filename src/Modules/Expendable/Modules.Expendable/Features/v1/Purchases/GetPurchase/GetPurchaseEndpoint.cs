using AMIS.Modules.Expendable.Contracts.v1.Purchases;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Purchases.GetPurchase;

public static class GetPurchaseEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", GetPurchase)
            .WithName(nameof(GetPurchaseQuery))
            .WithSummary("Get purchase order by ID")
            .Produces<PurchaseDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ExpendableModuleConstants.Permissions.Purchases.View);

    private static async Task<IResult> GetPurchase(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetPurchaseQuery(id);
        var result = await mediator.Send(query, cancellationToken);
        return result is not null ? TypedResults.Ok(result) : TypedResults.NotFound();
    }
}


