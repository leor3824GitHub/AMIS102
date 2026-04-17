using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableItems.GetSemiExpendableItemById;

public static class GetSemiExpendableItemByIdEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", GetItemCatalogById)
            .WithName(nameof(GetPropertyItemCatalogByIdQuery))
            .WithSummary("Get item catalog entry by ID")
            .Produces<PropertyItemCatalogDetailsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetManagementModuleConstants.Permissions.SemiExpendableItems.View);

    private static async Task<IResult> GetItemCatalogById(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPropertyItemCatalogByIdQuery(id), cancellationToken);
        return TypedResults.Ok(result);
    }
}
