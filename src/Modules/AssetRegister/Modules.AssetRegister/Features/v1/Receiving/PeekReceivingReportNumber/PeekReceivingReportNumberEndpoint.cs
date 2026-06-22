using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Receiving.PeekReceivingReportNumber;

public static class PeekReceivingReportNumberEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/next-number", Handle)
            .WithModuleName<PeekReceivingReportNumberQuery>()
            .WithSummary("Preview the next Receiving Report number (PPERR / SMRR) without consuming it")
            .Produces<string>()
            .RequirePermission(AssetRegisterPermissions.Receiving.Create);

    private static async Task<IResult> Handle(
        IMediator mediator,
        ReceivingDocumentKind kind,
        DateOnly date,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new PeekReceivingReportNumberQuery(kind, date), ct);
        return TypedResults.Ok(result);
    }
}
