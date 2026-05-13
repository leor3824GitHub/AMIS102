using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetManagement.Features.v1.InventoryCustodianSlips.GetICSById;

public static class GetICSByIdEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(new GetICSByIdQuery(id), ct)))
        .WithName(nameof(GetICSByIdQuery))
        .WithSummary("Get an Inventory Custodian Slip by ID, including its line items")
        .RequirePermission(AssetManagementModuleConstants.Permissions.InventoryCustodianSlips.View);
}

