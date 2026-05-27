using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetManagement.Contracts.Permissions;

namespace AMIS.Modules.AssetManagement.Features.v1.PropertyAcknowledgementReceipts.CreatePAR;

public static class CreatePAREndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", async ([FromBody] CreatePARCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return TypedResults.Created($"/api/v1/asset-management/property-acknowledgement-receipts/{result.PARId}", result);
        })
        .WithName(nameof(CreatePARCommand))
        .WithSummary("Create a Property Acknowledgement Receipt (PAR)")
        .RequirePermission(AssetManagementPermissions.PropertyAcknowledgementReceipts.Create);
}

