using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister;
using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetRegister.Contracts.Permissions;

namespace AMIS.Modules.AssetRegister.Features.v1.Receiving.UpdatePPERRFormSeries;

public static class UpdatePPERRFormSeriesEndpoint
{
    public sealed record UpdatePPERRFormSeriesRequest(int StartSerial, int EndSerial);

    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}", async (
            Guid id,
            UpdatePPERRFormSeriesRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new UpdatePPERRFormSeriesCommand(id, body.StartSerial, body.EndSerial), ct);
            return TypedResults.Ok(result);
        })
        .WithName("AssetRegister_UpdatePPERRFormSeries")
        .WithSummary("Update an unused PPERR Form Series serial range")
        .RequirePermission(AssetRegisterPermissions.Receiving.Create);
}
