using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetManagement.Features.v1.ReceiptsForReturnedPPE.CreateRRP;

public static class CreateRRPEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", async ([FromBody] CreateRRPCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return TypedResults.Created($"/api/v1/asset-management/receipts-for-returned-ppe/{result.RRPId}", result);
        })
        .WithName(nameof(CreateRRPCommand))
        .WithSummary("Create a Receipt for Returned Property (RRP)")
        .RequirePermission(AssetManagementModuleConstants.Permissions.ReceiptsForReturnedPPE.Create);
}

