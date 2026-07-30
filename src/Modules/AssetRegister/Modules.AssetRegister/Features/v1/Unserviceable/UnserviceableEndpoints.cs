using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Unserviceable;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetRegister.Contracts.Permissions;

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
            .WithModuleName<CreateUnserviceableReportDraftCommand>()
            .WithSummary("Create a draft unserviceable property report (IIRUSP/IIRUP)")
            .Produces<UnserviceablePropertyReportDto>(StatusCodes.Status201Created)
            .RequirePermission(AssetRegisterPermissions.Unserviceable.File);

        endpoints.MapPost("/{id:guid}/items", async (
                Guid id, AddUnserviceableReportItemCommand cmd, IMediator mediator, CancellationToken ct) =>
                TypedResults.Ok(await mediator.Send(cmd with { ReportId = id }, ct)))
            .WithModuleName<AddUnserviceableReportItemCommand>()
            .WithSummary("Add an item to a draft unserviceable report")
            .Produces<UnserviceablePropertyReportDto>()
            .RequirePermission(AssetRegisterPermissions.Unserviceable.File);

        endpoints.MapPut("/{id:guid}", async (
                Guid id, UpdateUnserviceableReportHeaderCommand cmd, IMediator mediator, CancellationToken ct) =>
                TypedResults.Ok(await mediator.Send(cmd with { ReportId = id }, ct)))
            .WithModuleName<UpdateUnserviceableReportHeaderCommand>()
            .WithSummary("Edit a draft unserviceable report's header (station, as-at, accountable officer)")
            .Produces<UnserviceablePropertyReportDto>()
            .RequirePermission(AssetRegisterPermissions.Unserviceable.File);

        endpoints.MapPost("/{id:guid}/submit", async (
                Guid id, SubmitUnserviceableReportCommand cmd, IMediator mediator, CancellationToken ct) =>
                TypedResults.Ok(await mediator.Send(cmd with { ReportId = id }, ct)))
            .WithModuleName<SubmitUnserviceableReportCommand>()
            .WithSummary("Submit a draft unserviceable report")
            .Produces<UnserviceablePropertyReportDto>()
            .RequirePermission(AssetRegisterPermissions.Unserviceable.File);

        endpoints.MapPost("/{id:guid}/inspection", async (
                Guid id, RecordUnserviceableInspectionCommand cmd, IMediator mediator, CancellationToken ct) =>
                TypedResults.Ok(await mediator.Send(cmd with { ReportId = id }, ct)))
            .WithModuleName<RecordUnserviceableInspectionCommand>()
            .WithSummary("Record inspection decisions per item")
            .Produces<UnserviceablePropertyReportDto>()
            .RequirePermission(AssetRegisterPermissions.Unserviceable.Dispose);

        endpoints.MapPost("/{id:guid}/disposal", async (
                Guid id, RecordUnserviceableDisposalCommand cmd, IMediator mediator, CancellationToken ct) =>
                TypedResults.Ok(await mediator.Send(cmd with { ReportId = id }, ct)))
            .WithModuleName<RecordUnserviceableDisposalCommand>()
            .WithSummary("Record disposal records â€” flips assets to Disposed")
            .Produces<UnserviceablePropertyReportDto>()
            .RequirePermission(AssetRegisterPermissions.Unserviceable.Dispose);

        endpoints.MapPost("/{id:guid}/close", async (Guid id, IMediator mediator, CancellationToken ct) =>
                TypedResults.Ok(await mediator.Send(new CloseUnserviceableReportCommand(id), ct)))
            .WithModuleName<CloseUnserviceableReportCommand>()
            .WithSummary("Close a fully disposed unserviceable report")
            .Produces<UnserviceablePropertyReportDto>()
            .RequirePermission(AssetRegisterPermissions.Unserviceable.Dispose);

        endpoints.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            {
                var r = await mediator.Send(new GetUnserviceableReportQuery(id), ct);
                return r is null ? (IResult)TypedResults.NotFound() : TypedResults.Ok(r);
            })
            .WithModuleName<GetUnserviceableReportQuery>()
            .WithSummary("Get an unserviceable report by id")
            .Produces<UnserviceablePropertyReportDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetRegisterPermissions.Unserviceable.View);

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
            .WithModuleName<SearchUnserviceableReportsQuery>()
            .WithSummary("Search unserviceable reports")
            .Produces<PagedResponse<UnserviceablePropertyReportSummaryDto>>()
            .RequirePermission(AssetRegisterPermissions.Unserviceable.View);

        return endpoints;
    }
}

