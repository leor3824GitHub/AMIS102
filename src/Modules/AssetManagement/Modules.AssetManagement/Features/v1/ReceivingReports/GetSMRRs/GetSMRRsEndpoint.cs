using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.ReceivingReports.GetSMRRs;

public static class GetSMRRsEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", async ([AsParameters] GetSMRRsQuery query, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(query, ct)))
        .WithName(nameof(GetSMRRsQuery))
        .WithSummary("Get a paginated list of Supplies and Materials Receiving Reports")
        .RequirePermission(AssetManagementModuleConstants.Permissions.ReceivingReports.View);
}
