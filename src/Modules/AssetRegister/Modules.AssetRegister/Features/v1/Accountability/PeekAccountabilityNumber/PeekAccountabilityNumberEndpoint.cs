using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.PeekAccountabilityNumber;

public static class PeekAccountabilityNumberEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/next-number", Handle)
            .WithModuleName<PeekAccountabilityNumberQuery>()
            .WithSummary("Preview the next ICS / PAR document number without consuming it")
            .Produces<string>()
            .RequirePermission(AssetRegisterPermissions.Accountability.Issue);

    private static async Task<IResult> Handle(
        IMediator mediator,
        AccountabilityType type,
        DateOnly date,
        bool highValued = false,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new PeekAccountabilityNumberQuery(type, date, highValued), ct);
        return TypedResults.Ok(result);
    }
}
