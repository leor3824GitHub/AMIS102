using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetManagement.Features.v1.TangibleInventory.UpdateTangibleInventory;

public static class UpdateTangibleInventoryEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateTangibleInventoryCommand command, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(command with { Id = id }, ct);
            return TypedResults.NoContent();
        })
        .WithName(nameof(UpdateTangibleInventoryCommand))
        .WithSummary("Update a Tangible Inventory receiving report (header fields)")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .RequirePermission(AssetManagementModuleConstants.Permissions.TangibleInventory.Update);
}

