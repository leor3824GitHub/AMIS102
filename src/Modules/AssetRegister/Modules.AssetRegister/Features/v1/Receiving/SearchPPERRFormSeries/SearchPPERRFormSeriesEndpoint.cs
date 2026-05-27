using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister;
using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetRegister.Contracts.Permissions;

namespace AMIS.Modules.AssetRegister.Features.v1.Receiving.SearchPPERRFormSeries;

public static class SearchPPERRFormSeriesEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", async (
            [AsParameters] SearchPPERRFormSeriesQuery query,
            IMediator mediator,
            CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(query, ct)))
        .WithName("AssetRegister_SearchPPERRFormSeries")
        .WithSummary("List all PPERR Form Series batches")
        .RequirePermission(AssetRegisterPermissions.Receiving.View);
}
