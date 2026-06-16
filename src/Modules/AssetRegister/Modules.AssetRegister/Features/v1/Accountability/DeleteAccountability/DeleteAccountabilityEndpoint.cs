using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetRegister.Contracts.Permissions;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.DeleteAccountability;

public static class DeleteAccountabilityEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapDelete("/{id:guid}", Handle)
            .WithModuleName<DeleteAccountabilityCommand>()
            .WithSummary("Delete a still-pending ICS/PAR and release its reserved assets")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission(AssetRegisterPermissions.Accountability.Delete);

    private static async Task<IResult> Handle(Guid id, IMediator mediator, CancellationToken ct)
    {
        await mediator.Send(new DeleteAccountabilityCommand(id), ct);
        return TypedResults.NoContent();
    }
}
