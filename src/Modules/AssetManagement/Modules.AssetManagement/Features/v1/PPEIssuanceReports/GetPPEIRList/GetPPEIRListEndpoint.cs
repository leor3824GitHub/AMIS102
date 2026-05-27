using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetManagement.Contracts.Permissions;

namespace AMIS.Modules.AssetManagement.Features.v1.PPEIssuanceReports.GetPPEIRList;

public static class GetPPEIRListEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", async (
            [AsParameters] GetPPEIRListQuery query,
            IMediator mediator,
            CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(query, ct)))
        .WithName(nameof(GetPPEIRListQuery))
        .WithSummary("Get a paged list of PPE Issuance Reports")
        .RequirePermission(AssetManagementPermissions.PPEIssuanceReports.View);
}

