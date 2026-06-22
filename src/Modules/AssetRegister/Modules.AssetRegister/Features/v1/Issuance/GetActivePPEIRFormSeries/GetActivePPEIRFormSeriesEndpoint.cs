using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister;
using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetRegister.Contracts.Permissions;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance.GetActivePPEIRFormSeries;

public static class GetActivePPEIRFormSeriesEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/active", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetActivePPEIRFormSeriesQuery(), ct);
            return result is null ? Results.NoContent() : Results.Ok(result);
        })
        .WithName("AssetRegister_GetActivePPEIRFormSeries")
        .WithSummary("Get the currently active PPEIR Form Series")
        .RequirePermission(AssetRegisterPermissions.Issuance.View);
}
