using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister;
using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetRegister.Contracts.Permissions;

namespace AMIS.Modules.AssetRegister.Features.v1.Receiving.CreatePPERRFormSeries;

public static class CreatePPERRFormSeriesEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", async (
            CreatePPERRFormSeriesCommand cmd,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd, ct);
            return TypedResults.Created($"/api/v1/asset-register/pperr-series/{result.Id}", result);
        })
        .WithName("AssetRegister_CreatePPERRFormSeries")
        .WithSummary("Register a new batch of pre-printed PPERR accountable form numbers")
        .RequirePermission(AssetRegisterPermissions.Receiving.Create);
}
