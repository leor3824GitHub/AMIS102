using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.TangibleInventory.DeleteTangibleInventory;

public static class DeleteTangibleInventoryEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new DeleteTangibleInventoryCommand(id), ct);
            return TypedResults.NoContent();
        })
        .WithName(nameof(DeleteTangibleInventoryCommand))
        .WithSummary("Soft-delete a Tangible Inventory receiving report")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .RequirePermission(AssetManagementModuleConstants.Permissions.TangibleInventory.Delete);
}
