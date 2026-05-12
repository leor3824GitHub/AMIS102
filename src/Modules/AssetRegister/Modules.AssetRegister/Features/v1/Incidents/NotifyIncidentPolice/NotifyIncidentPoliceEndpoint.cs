using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.AssetRegister.Contracts.v1.Incidents;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetRegister.Features.v1.Incidents.NotifyIncidentPolice;

public static class NotifyIncidentPoliceEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/police", Handle)
            .WithName(nameof(NotifyIncidentPoliceCommand))
            .WithSummary("Record police notification on an incident report")
            .Produces<PropertyIncidentReportDto>()
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Incident.Resolve);

    private static async Task<IResult> Handle(
        Guid id, NotifyIncidentPoliceCommand cmd, IMediator mediator, CancellationToken ct)
    {
        if (id != cmd.IncidentReportId) return TypedResults.BadRequest("Route id and body id must match.");
        var result = await mediator.Send(cmd, ct);
        return TypedResults.Ok(result);
    }
}
