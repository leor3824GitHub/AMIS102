using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.PPEIssuanceReports.CreatePPEIR;

public static class CreatePPEIREndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", async ([FromBody] CreatePPEIRCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return TypedResults.Created($"/api/v1/asset-management/ppe-issuance-reports/{result.PPEIRId}", result);
        })
        .WithName(nameof(CreatePPEIRCommand))
        .WithSummary("Create a PPE Issuance Report (PPEIR) — inter-office/department transfer")
        .RequirePermission(AssetManagementModuleConstants.Permissions.PPEIssuanceReports.Create);
}
