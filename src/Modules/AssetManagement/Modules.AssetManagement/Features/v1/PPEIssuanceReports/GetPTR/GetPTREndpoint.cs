using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetManagement.Contracts.Permissions;

namespace AMIS.Modules.AssetManagement.Features.v1.PPEIssuanceReports.GetPTR;

public static class GetPTREndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{ppeirId:guid}/ptr", async (Guid ppeirId, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(new GetPTRQuery(ppeirId), ct)))
        .WithName(nameof(GetPTRQuery))
        .WithSummary("Get Property Transfer Report (PTR) derived from a PPEIR")
        .RequirePermission(AssetManagementPermissions.PPEIssuanceReports.View);
}

