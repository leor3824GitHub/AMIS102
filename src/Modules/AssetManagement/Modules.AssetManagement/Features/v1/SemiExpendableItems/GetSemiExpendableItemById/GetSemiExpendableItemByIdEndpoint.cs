using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableItems.GetSemiExpendableItemById;

public static class GetSemiExpendableItemByIdEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", GetSemiExpendableItemById)
            .WithName(nameof(GetSemiExpendableItemByIdQuery))
            .WithSummary("Get semi-expendable item catalog entry by ID")
            .Produces<SemiExpendableItemDetailsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetManagementModuleConstants.Permissions.SemiExpendableItems.View);

    private static async Task<IResult> GetSemiExpendableItemById(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetSemiExpendableItemByIdQuery(id), cancellationToken);
        return TypedResults.Ok(result);
    }
}
