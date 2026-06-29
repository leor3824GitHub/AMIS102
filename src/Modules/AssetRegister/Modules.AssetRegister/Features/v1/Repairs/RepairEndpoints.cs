using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using AMIS.Modules.AssetRegister.Contracts.v1.Repairs;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Repairs;

public static class RepairEndpoints
{
    public static void MapRepairEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost("/", async (RequestRepairCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd, ct);
            return TypedResults.Created($"/api/v1/asset-register/repairs/{result.Id}", result);
        })
        .WithName("AssetRegister_RequestRepair")
        .WithSummary("Request a PPE repair (RPRI / Exhibit 6)")
        .Produces<PropertyRepairDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .RequirePermission(AssetRegisterPermissions.Repair.Request);

        endpoints.MapPost("/{id:guid}/pre-inspection", async (Guid id, RecordPreRepairInspectionCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd with { RepairId = id }, ct);
            return TypedResults.Ok(result);
        })
        .WithName("AssetRegister_RecordPreRepairInspection")
        .WithSummary("Record the mandatory pre-repair inspection")
        .Produces<PropertyRepairDto>(StatusCodes.Status200OK)
        .RequirePermission(AssetRegisterPermissions.Repair.Inspect);

        endpoints.MapPost("/{id:guid}/post-inspection", async (Guid id, RecordPostRepairInspectionCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd with { RepairId = id }, ct);
            return TypedResults.Ok(result);
        })
        .WithName("AssetRegister_RecordPostRepairInspection")
        .WithSummary("Record the post-repair inspection (+ optional PO-JO / BUR / DV refs)")
        .Produces<PropertyRepairDto>(StatusCodes.Status200OK)
        .RequirePermission(AssetRegisterPermissions.Repair.Inspect);

        endpoints.MapPost("/{id:guid}/accept", async (Guid id, AcceptRepairCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd with { RepairId = id }, ct);
            return TypedResults.Ok(result);
        })
        .WithName("AssetRegister_AcceptRepair")
        .WithSummary("Property custodian accepts the repair — locks the RPRI as history")
        .Produces<PropertyRepairDto>(StatusCodes.Status200OK)
        .RequirePermission(AssetRegisterPermissions.Repair.Accept);

        endpoints.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetPropertyRepairQuery(id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("AssetRegister_GetPropertyRepair")
        .WithSummary("Get a repair (RPRI) by id")
        .Produces<PropertyRepairDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .RequirePermission(AssetRegisterPermissions.Repair.View);

        endpoints.MapGet("/", async ([AsParameters] SearchPropertyRepairsQuery query, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(query, ct);
            return TypedResults.Ok(result);
        })
        .WithName("AssetRegister_SearchPropertyRepairs")
        .WithSummary("Search repairs (RPRIs)")
        .RequirePermission(AssetRegisterPermissions.Repair.View);

        endpoints.MapGet("/history/{assetRegistryId:guid}", async (Guid assetRegistryId, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetAssetRepairHistoryQuery(assetRegistryId), ct);
            return TypedResults.Ok(result);
        })
        .WithName("AssetRegister_GetAssetRepairHistory")
        .WithSummary("Accepted repair history (RPRIs) of an asset")
        .RequirePermission(AssetRegisterPermissions.Repair.View);

        endpoints.MapGet("/last/{assetRegistryId:guid}", async (Guid assetRegistryId, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetLastRepairQuery(assetRegistryId), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("AssetRegister_GetLastRepair")
        .WithSummary("Most recent accepted repair of an asset")
        .Produces<PropertyRepairDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .RequirePermission(AssetRegisterPermissions.Repair.View);
    }
}
