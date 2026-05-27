using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetManagement.Contracts.Permissions;

namespace AMIS.Modules.AssetManagement.Features.v1.ReceiptsForReturnedPPE.GetRRPList;

public static class GetRRPListEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", async (
            [AsParameters] GetRRPListQuery query,
            IMediator mediator,
            CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(query, ct)))
        .WithName(nameof(GetRRPListQuery))
        .WithSummary("Get a paged list of Receipts for Returned PPE")
        .RequirePermission(AssetManagementPermissions.ReceiptsForReturnedPPE.View);
}

