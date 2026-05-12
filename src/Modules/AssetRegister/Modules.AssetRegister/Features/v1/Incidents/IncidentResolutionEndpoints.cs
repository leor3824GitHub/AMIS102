using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.AssetRegister.Contracts.v1.Incidents;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetRegister.Features.v1.Incidents;

public static class IncidentResolutionEndpoints
{
    public static IEndpointRouteBuilder MapResolutionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/{id:guid}/recovery", async (
                Guid id, RecordIncidentRecoveryCommand cmd, IMediator mediator, CancellationToken ct) =>
            {
                if (id != cmd.IncidentReportId) return TypedResults.BadRequest("Route id and body id must match.");
                return (IResult)TypedResults.Ok(await mediator.Send(cmd, ct));
            })
            .WithName(nameof(RecordIncidentRecoveryCommand))
            .WithSummary("Record asset recovery on an incident report")
            .Produces<PropertyIncidentReportDto>()
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Incident.Resolve);

        endpoints.MapPost("/{id:guid}/settlement", async (
                Guid id, RecordIncidentSettlementCommand cmd, IMediator mediator, CancellationToken ct) =>
            {
                if (id != cmd.IncidentReportId) return TypedResults.BadRequest("Route id and body id must match.");
                return (IResult)TypedResults.Ok(await mediator.Send(cmd, ct));
            })
            .WithName(nameof(RecordIncidentSettlementCommand))
            .WithSummary("Record monetary settlement (accountable officer paid)")
            .Produces<PropertyIncidentReportDto>()
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Incident.Resolve);

        endpoints.MapPost("/{id:guid}/relief", async (
                Guid id, GrantIncidentReliefCommand cmd, IMediator mediator, CancellationToken ct) =>
            {
                if (id != cmd.IncidentReportId) return TypedResults.BadRequest("Route id and body id must match.");
                return (IResult)TypedResults.Ok(await mediator.Send(cmd, ct));
            })
            .WithName(nameof(GrantIncidentReliefCommand))
            .WithSummary("Record relief granted (COA decision)")
            .Produces<PropertyIncidentReportDto>()
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Incident.Resolve);

        endpoints.MapPost("/{id:guid}/derecognize", async (
                Guid id, DerecognizeIncidentItemCommand cmd, IMediator mediator, CancellationToken ct) =>
            {
                if (id != cmd.IncidentReportId) return TypedResults.BadRequest("Route id and body id must match.");
                return (IResult)TypedResults.Ok(await mediator.Send(cmd, ct));
            })
            .WithName(nameof(DerecognizeIncidentItemCommand))
            .WithSummary("Derecognize an item (COA Circular 2020-006 §8 — requires authority)")
            .Produces<PropertyIncidentReportDto>()
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Incident.Resolve);

        endpoints.MapPost("/{id:guid}/close", async (Guid id, IMediator mediator, CancellationToken ct) =>
                TypedResults.Ok(await mediator.Send(new CloseIncidentReportCommand(id), ct)))
            .WithName(nameof(CloseIncidentReportCommand))
            .WithSummary("Close a fully resolved incident report")
            .Produces<PropertyIncidentReportDto>()
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Incident.Resolve);

        return endpoints;
    }
}
