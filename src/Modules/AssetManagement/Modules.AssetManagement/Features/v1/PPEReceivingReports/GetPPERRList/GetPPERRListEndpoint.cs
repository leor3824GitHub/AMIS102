using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.PPEReceivingReports.GetPPERRList;

public static class GetPPERRListEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", async (
            [AsParameters] GetPPERRListQuery query,
            IMediator mediator,
            CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(query, ct)))
        .WithName(nameof(GetPPERRListQuery))
        .WithSummary("Get a paged list of PPE Receiving Reports")
        .RequirePermission(AssetManagementModuleConstants.Permissions.PPEReceivingReports.View);
}
