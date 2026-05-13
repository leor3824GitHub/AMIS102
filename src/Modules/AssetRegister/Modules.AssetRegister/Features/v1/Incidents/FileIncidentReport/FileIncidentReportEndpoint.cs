using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.AssetRegister.Contracts.v1.Incidents;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetRegister.Features.v1.Incidents.FileIncidentReport;

public static class FileIncidentReportEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", Handle)
            .WithName(nameof(FileIncidentReportCommand))
            .WithSummary("File a property incident report (RLSDDSP)")
            .Produces<PropertyIncidentReportDto>(StatusCodes.Status201Created)
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Incident.File);

    private static async Task<IResult> Handle(
        FileIncidentReportCommand cmd, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(cmd, ct);
        return TypedResults.Created($"/api/v1/asset-register/incidents/{result.Id}", result);
    }
}
