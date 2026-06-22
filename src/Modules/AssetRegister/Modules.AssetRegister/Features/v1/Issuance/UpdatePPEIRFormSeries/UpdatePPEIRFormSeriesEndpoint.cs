using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister;
using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetRegister.Contracts.Permissions;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance.UpdatePPEIRFormSeries;

public static class UpdatePPEIRFormSeriesEndpoint
{
    public sealed record UpdatePPEIRFormSeriesRequest(string Label, int StartSerial, int EndSerial);

    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}", async (
            Guid id,
            UpdatePPEIRFormSeriesRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new UpdatePPEIRFormSeriesCommand(id, body.Label, body.StartSerial, body.EndSerial), ct);
            return TypedResults.Ok(result);
        })
        .WithName("AssetRegister_UpdatePPEIRFormSeries")
        .WithSummary("Update an unused PPEIR Form Series label and serial range")
        .RequirePermission(AssetRegisterPermissions.Issuance.Create);
}
