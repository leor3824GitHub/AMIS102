using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetManagement.Contracts.Permissions;

namespace AMIS.Modules.AssetManagement.Features.v1.InventoryCustodianSlips.GetICSList;

public static class GetICSListEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", async ([AsParameters] GetICSListQuery query, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(query, ct)))
        .WithName(nameof(GetICSListQuery))
        .WithSummary("Get a paginated list of Inventory Custodian Slips")
        .RequirePermission(AssetManagementPermissions.InventoryCustodianSlips.View);
}

