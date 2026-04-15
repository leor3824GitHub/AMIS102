using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.PPEIssuanceReports.GetPTR;

public static class GetPTREndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{ppeirId:guid}/ptr", async (Guid ppeirId, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(new GetPTRQuery(ppeirId), ct)))
        .WithName(nameof(GetPTRQuery))
        .WithSummary("Get Property Transfer Report (PTR) derived from a PPEIR")
        .RequirePermission(AssetManagementModuleConstants.Permissions.PPEIssuanceReports.View);
}
