using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister;
using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetRegister.Contracts.Permissions;

namespace AMIS.Modules.AssetRegister.Features.v1.Receiving.ActivatePPERRFormSeries;

public static class ActivatePPERRFormSeriesEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}/activate", async (
            Guid id,
            IMediator mediator,
            CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(new ActivatePPERRFormSeriesCommand(id), ct)))
        .WithName("AssetRegister_ActivatePPERRFormSeries")
        .WithSummary("Set a PPERR Form Series as the active series for new report numbers")
        .RequirePermission(AssetRegisterPermissions.Receiving.Create);

    public static RouteHandlerBuilder MapDeactivate(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}/deactivate", async (
            Guid id,
            IMediator mediator,
            CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(new DeactivatePPERRFormSeriesCommand(id), ct)))
        .WithName("AssetRegister_DeactivatePPERRFormSeries")
        .WithSummary("Deactivate a PPERR Form Series")
        .RequirePermission(AssetRegisterPermissions.Receiving.Create);
}
