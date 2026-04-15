using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.PropertyAcknowledgementReceipts.GetPARList;

public static class GetPARListEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", async (
            [AsParameters] GetPARListQuery query,
            IMediator mediator,
            CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(query, ct)))
        .WithName(nameof(GetPARListQuery))
        .WithSummary("Get a paged list of Property Acknowledgement Receipts")
        .RequirePermission(AssetManagementModuleConstants.Permissions.PropertyAcknowledgementReceipts.View);
}
