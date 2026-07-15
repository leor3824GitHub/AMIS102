using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.GetExpiringAccountabilities;

public static class GetExpiringAccountabilitiesEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/expiring", Handle)
            .WithModuleName<GetExpiringAccountabilitiesQuery>()
            .WithSummary("Active ICS/PAR accountabilities due (or overdue) for renewal within N days")
            .Produces<IReadOnlyList<PropertyAccountabilitySummaryDto>>()
            .RequirePermission(AssetRegisterPermissions.Accountability.View);

    private static async Task<IResult> Handle(
        IMediator mediator,
        int withinDays = 60,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetExpiringAccountabilitiesQuery(withinDays), ct);
        return TypedResults.Ok(result);
    }
}
