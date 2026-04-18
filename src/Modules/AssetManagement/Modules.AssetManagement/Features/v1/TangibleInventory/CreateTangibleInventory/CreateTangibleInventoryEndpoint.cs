using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.TangibleInventory.CreateTangibleInventory;

public static class CreateTangibleInventoryEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", async ([FromBody] CreateTangibleInventoryCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return TypedResults.Created($"/api/v1/asset-management/tangible-inventories/{result.TangibleInventoryId}", result);
        })
        .WithName(nameof(CreateTangibleInventoryCommand))
        .WithSummary("Create a Tangible Inventory report and register received tangible assets (SE and PPE)")
        .RequirePermission(AssetManagementModuleConstants.Permissions.TangibleInventory.Create);
}
