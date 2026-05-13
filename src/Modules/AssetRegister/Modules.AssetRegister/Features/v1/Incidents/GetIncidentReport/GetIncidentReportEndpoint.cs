using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.AssetRegister.Contracts.v1.Incidents;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetRegister.Features.v1.Incidents.GetIncidentReport;

public static class GetIncidentReportEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", Handle)
            .WithName(nameof(GetIncidentReportQuery))
            .WithSummary("Get an incident report by id")
            .Produces<PropertyIncidentReportDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Incident.View);

    private static async Task<IResult> Handle(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetIncidentReportQuery(id), ct);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }
}
