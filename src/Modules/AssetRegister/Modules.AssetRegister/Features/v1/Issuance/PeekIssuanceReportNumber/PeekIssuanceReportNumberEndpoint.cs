using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance.PeekIssuanceReportNumber;

public static class PeekIssuanceReportNumberEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/next-number", Handle)
            .WithModuleName<PeekIssuanceReportNumberQuery>()
            .WithSummary("Preview the next Issuance Report number (PPEIR / SMIR) without consuming it")
            .Produces<string>()
            .RequirePermission(AssetRegisterPermissions.Issuance.Create);

    private static async Task<IResult> Handle(
        IMediator mediator,
        IssuanceReportType type,
        DateOnly date,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new PeekIssuanceReportNumberQuery(type, date), ct);
        return TypedResults.Ok(result);
    }
}
