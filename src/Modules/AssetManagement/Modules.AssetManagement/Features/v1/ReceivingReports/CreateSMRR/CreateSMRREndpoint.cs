using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.ReceivingReports.CreateSMRR;

public static class CreateSMRREndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", async ([FromBody] CreateSMRRCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return TypedResults.Created($"/api/v1/asset-management/receiving-reports/{result.SMRRId}", result);
        })
        .WithName(nameof(CreateSMRRCommand))
        .WithSummary("Create a Supplies and Materials Receiving Report (SMRR) and register received property units")
        .RequirePermission(AssetManagementModuleConstants.Permissions.ReceivingReports.Create);
}
