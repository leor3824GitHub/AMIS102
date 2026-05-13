using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetManagement.Features.v1.TangibleInventory.GetTangibleInventories;

public static class GetTangibleInventoriesEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", async (
            [AsParameters] GetTangibleInventoriesQuery query,
            IMediator mediator,
            CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(query, ct)))
        .WithName(nameof(GetTangibleInventoriesQuery))
        .WithSummary("Get paginated list of Tangible Inventory reports with optional filters")
        .RequirePermission(AssetManagementModuleConstants.Permissions.TangibleInventory.View);
}

