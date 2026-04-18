using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.TangibleInventory.GetTangibleInventoryById;

public static class GetTangibleInventoryByIdEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetTangibleInventoryByIdQuery(id), ct);
            return TypedResults.Ok(result);
        })
        .WithName(nameof(GetTangibleInventoryByIdQuery))
        .WithSummary("Get a Tangible Inventory report with all line items by ID")
        .RequirePermission(AssetManagementModuleConstants.Permissions.TangibleInventory.View);
}
