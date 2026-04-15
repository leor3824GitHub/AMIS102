using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.PPEReceivingReports.CreatePPERR;

public static class CreatePPERREndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", async ([FromBody] CreatePPERRCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return TypedResults.Created($"/api/v1/asset-management/ppe-receiving-reports/{result.PPERRId}", result);
        })
        .WithName(nameof(CreatePPERRCommand))
        .WithSummary("Create a PPE Receiving Report (PPERR)")
        .RequirePermission(AssetManagementModuleConstants.Permissions.PPEReceivingReports.Create);
}
