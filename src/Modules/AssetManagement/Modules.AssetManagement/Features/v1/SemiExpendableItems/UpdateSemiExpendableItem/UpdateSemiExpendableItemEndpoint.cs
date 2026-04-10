using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableItems.UpdateSemiExpendableItem;

public static class UpdateSemiExpendableItemEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}", UpdateSemiExpendableItem)
            .WithName(nameof(UpdateSemiExpendableItemCommand))
            .WithSummary("Update semi-expendable item catalog entry")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetManagementModuleConstants.Permissions.SemiExpendableItems.Update);

    private static async Task<IResult> UpdateSemiExpendableItem(
        Guid id,
        UpdateSemiExpendableItemCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(command with { Id = id }, cancellationToken);
        return TypedResults.NoContent();
    }
}
