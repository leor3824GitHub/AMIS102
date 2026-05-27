using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister;
using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetRegister.Contracts.Permissions;

namespace AMIS.Modules.AssetRegister.Features.v1.Receiving.DeletePPERRFormSeries;

public static class DeletePPERRFormSeriesEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapDelete("/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(new DeletePPERRFormSeriesCommand(id), ct);
            return TypedResults.NoContent();
        })
        .WithName("AssetRegister_DeletePPERRFormSeries")
        .WithSummary("Delete an unused PPERR Form Series")
        .RequirePermission(AssetRegisterPermissions.Receiving.Delete);
}
