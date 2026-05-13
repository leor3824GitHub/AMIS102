using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Unserviceable;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Unserviceable;

public static class UnserviceableEndpoints
{
    public static IEndpointRouteBuilder MapUnserviceableEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/", async (
                CreateUnserviceableReportDraftCommand cmd, IMediator mediator, CancellationToken ct) =>
            {
                var r = await mediator.Send(cmd, ct);
                return TypedResults.Created($"/api/v1/asset-register/unserviceable/{r.Id}", r);
            })
            .WithName("AssetRegister_CreateUnserviceableReportDraft")
            .WithSummary("Create a draft unserviceable property report (IIRUSP/IIRUP)")
            .Produces<UnserviceablePropertyReportDto>(StatusCodes.Status201Created)
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Unserviceable.File);

        endpoints.MapPost("/{id:guid}/items", async (
                Guid id, AddUnserviceableReportItemCommand cmd, IMediator mediator, CancellationToken ct) =>
            {
                if (id != cmd.ReportId) return TypedResults.BadRequest("Route id and body id must match.");
                return (IResult)TypedResults.Ok(await mediator.Send(cmd, ct));
            })
            .WithName("AssetRegister_AddUnserviceableReportItem")
            .WithSummary("Add an item to a draft unserviceable report")
            .Produces<UnserviceablePropertyReportDto>()
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Unserviceable.File);

        endpoints.MapPost("/{id:guid}/submit", async (
                Guid id, SubmitUnserviceableReportCommand cmd, IMediator mediator, CancellationToken ct) =>
            {
                if (id != cmd.ReportId) return TypedResults.BadRequest("Route id and body id must match.");
                return (IResult)TypedResults.Ok(await mediator.Send(cmd, ct));
            })
            .WithName("AssetRegister_SubmitUnserviceableReport")
            .WithSummary("Submit a draft unserviceable report")
            .Produces<UnserviceablePropertyReportDto>()
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Unserviceable.File);

        endpoints.MapPost("/{id:guid}/inspection", async (
                Guid id, RecordUnserviceableInspectionCommand cmd, IMediator mediator, CancellationToken ct) =>
            {
                if (id != cmd.ReportId) return TypedResults.BadRequest("Route id and body id must match.");
                return (IResult)TypedResults.Ok(await mediator.Send(cmd, ct));
            })
            .WithName("AssetRegister_RecordUnserviceableInspection")
            .WithSummary("Record inspection decisions per item")
            .Produces<UnserviceablePropertyReportDto>()
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Unserviceable.Dispose);

        endpoints.MapPost("/{id:guid}/disposal", async (
                Guid id, RecordUnserviceableDisposalCommand cmd, IMediator mediator, CancellationToken ct) =>
            {
                if (id != cmd.ReportId) return TypedResults.BadRequest("Route id and body id must match.");
                return (IResult)TypedResults.Ok(await mediator.Send(cmd, ct));
            })
            .WithName("AssetRegister_RecordUnserviceableDisposal")
            .WithSummary("Record disposal records — flips assets to Disposed")
            .Produces<UnserviceablePropertyReportDto>()
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Unserviceable.Dispose);

        endpoints.MapPost("/{id:guid}/close", async (Guid id, IMediator mediator, CancellationToken ct) =>
                TypedResults.Ok(await mediator.Send(new CloseUnserviceableReportCommand(id), ct)))
            .WithName("AssetRegister_CloseUnserviceableReport")
            .WithSummary("Close a fully disposed unserviceable report")
            .Produces<UnserviceablePropertyReportDto>()
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Unserviceable.Dispose);

        endpoints.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            {
                var r = await mediator.Send(new GetUnserviceableReportQuery(id), ct);
                return r is null ? (IResult)TypedResults.NotFound() : TypedResults.Ok(r);
            })
            .WithName("AssetRegister_GetUnserviceableReport")
            .WithSummary("Get an unserviceable report by id")
            .Produces<UnserviceablePropertyReportDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Unserviceable.View);

        endpoints.MapGet("/", async (
                IMediator mediator,
                string? keyword = null,
                UnserviceableReportType? reportType = null,
                UnserviceableReportStatus? status = null,
                DateOnly? fromDate = null,
                DateOnly? toDate = null,
                int pageNumber = 1,
                int pageSize = 10,
                CancellationToken ct = default) =>
            {
                var r = await mediator.Send(new SearchUnserviceableReportsQuery(
                    keyword, reportType, status, fromDate, toDate, pageNumber, pageSize), ct);
                return TypedResults.Ok(r);
            })
            .WithName("AssetRegister_SearchUnserviceableReports")
            .WithSummary("Search unserviceable reports")
            .Produces<PagedResponse<UnserviceablePropertyReportSummaryDto>>()
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Unserviceable.View);

        return endpoints;
    }
}

