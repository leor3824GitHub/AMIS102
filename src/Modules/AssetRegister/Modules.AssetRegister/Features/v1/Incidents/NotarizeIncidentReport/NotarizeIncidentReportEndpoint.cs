using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.v1.Incidents;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Incidents.NotarizeIncidentReport;

public static class NotarizeIncidentReportEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/notarize", Handle)
            .WithModuleName<NotarizeIncidentReportCommand>()
            .WithSummary("Record notarization on an RLSDDSP")
            .Produces<PropertyIncidentReportDto>()
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Incident.Resolve);

    private static async Task<IResult> Handle(
        Guid id, NotarizeIncidentReportCommand cmd, IMediator mediator, CancellationToken ct)
    {
        if (id != cmd.IncidentReportId) return TypedResults.BadRequest("Route id and body id must match.");
        var result = await mediator.Send(cmd, ct);
        return TypedResults.Ok(result);
    }
}

