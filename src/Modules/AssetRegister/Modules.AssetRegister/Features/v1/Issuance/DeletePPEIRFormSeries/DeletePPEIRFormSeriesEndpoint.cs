using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister;
using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetRegister.Contracts.Permissions;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance.DeletePPEIRFormSeries;

public static class DeletePPEIRFormSeriesEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapDelete("/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(new DeletePPEIRFormSeriesCommand(id), ct);
            return TypedResults.NoContent();
        })
        .WithName("AssetRegister_DeletePPEIRFormSeries")
        .WithSummary("Delete an unused PPEIR Form Series")
        .RequirePermission(AssetRegisterPermissions.Issuance.Update);
}
