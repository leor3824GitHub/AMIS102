using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetManagement.Features.v1.InventoryCustodianSlips.CreateICS;

public static class CreateICSEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", async ([FromBody] CreateICSCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return TypedResults.Created($"/api/v1/asset-management/inventory-custodian-slips/{result.ICSId}", result);
        })
        .WithName(nameof(CreateICSCommand))
        .WithSummary("Create an Inventory Custodian Slip (ICS), issuing semi-expendable property to an end-user")
        .RequirePermission(AssetManagementModuleConstants.Permissions.InventoryCustodianSlips.Create);
}

