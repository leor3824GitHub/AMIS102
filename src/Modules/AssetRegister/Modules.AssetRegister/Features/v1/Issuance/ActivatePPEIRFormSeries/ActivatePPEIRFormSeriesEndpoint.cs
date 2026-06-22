using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister;
using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetRegister.Contracts.Permissions;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance.ActivatePPEIRFormSeries;

public static class ActivatePPEIRFormSeriesEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}/activate", async (
            Guid id,
            IMediator mediator,
            CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(new ActivatePPEIRFormSeriesCommand(id), ct)))
        .WithName("AssetRegister_ActivatePPEIRFormSeries")
        .WithSummary("Set a PPEIR Form Series as the active series for new report numbers")
        .RequirePermission(AssetRegisterPermissions.Issuance.Create);

    public static RouteHandlerBuilder MapDeactivate(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}/deactivate", async (
            Guid id,
            IMediator mediator,
            CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(new DeactivatePPEIRFormSeriesCommand(id), ct)))
        .WithName("AssetRegister_DeactivatePPEIRFormSeries")
        .WithSummary("Deactivate a PPEIR Form Series")
        .RequirePermission(AssetRegisterPermissions.Issuance.Create);
}
