using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse.IssueFromProductInventory;

public static class IssueFromProductInventoryEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/inventory/{inventoryId:guid}/issue", IssueInventory)
            .WithName(nameof(IssueFromProductInventoryCommand))
            .WithSummary("Issue product from reserved inventory using moving-average valuation")
            .Produces<IssueFromProductInventoryResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ExpendableModuleConstants.Permissions.Inventory.Consume);

    private static async Task<IResult> IssueInventory(
        Guid inventoryId,
        IssueFromProductInventoryCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var cmd = command with { ProductInventoryId = inventoryId };
        var result = await mediator.Send(cmd, cancellationToken);
        return TypedResults.Ok(result);
    }
}


