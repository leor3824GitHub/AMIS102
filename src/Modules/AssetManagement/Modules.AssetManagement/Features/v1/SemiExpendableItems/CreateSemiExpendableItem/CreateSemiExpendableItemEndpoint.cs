using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableItems.CreateSemiExpendableItem;

public static class CreateSemiExpendableItemEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", CreateItemCatalogEntry)
            .WithName(nameof(CreatePropertyItemCatalogCommand))
            .WithSummary("Create item catalog entry (SE or PPE)")
            .Produces<PropertyItemCatalogDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(AssetManagementModuleConstants.Permissions.SemiExpendableItems.Create);

    private static async Task<IResult> CreateItemCatalogEntry(
        CreatePropertyItemCatalogCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return TypedResults.Created($"/api/v1/asset-management/item-catalog/{result.Id}", result);
    }
}
