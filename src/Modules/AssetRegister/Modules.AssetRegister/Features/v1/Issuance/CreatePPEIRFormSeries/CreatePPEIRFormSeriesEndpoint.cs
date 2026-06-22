using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister;
using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetRegister.Contracts.Permissions;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance.CreatePPEIRFormSeries;

public static class CreatePPEIRFormSeriesEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", async (
            CreatePPEIRFormSeriesCommand cmd,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd, ct);
            return TypedResults.Created($"/api/v1/asset-register/ppeir-series/{result.Id}", result);
        })
        .WithName("AssetRegister_CreatePPEIRFormSeries")
        .WithSummary("Register a new batch of pre-printed PPEIR accountable form numbers")
        .RequirePermission(AssetRegisterPermissions.Issuance.Create);
}
