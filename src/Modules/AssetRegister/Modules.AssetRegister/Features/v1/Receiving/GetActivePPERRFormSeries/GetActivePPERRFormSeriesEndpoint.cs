using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister;
using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetRegister.Contracts.Permissions;

namespace AMIS.Modules.AssetRegister.Features.v1.Receiving.GetActivePPERRFormSeries;

public static class GetActivePPERRFormSeriesEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/active", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetActivePPERRFormSeriesQuery(), ct);
            return result is null ? Results.NoContent() : Results.Ok(result);
        })
        .WithName("AssetRegister_GetActivePPERRFormSeries")
        .WithSummary("Get the currently active PPERR Form Series")
        .RequirePermission(AssetRegisterPermissions.Receiving.View);
}
