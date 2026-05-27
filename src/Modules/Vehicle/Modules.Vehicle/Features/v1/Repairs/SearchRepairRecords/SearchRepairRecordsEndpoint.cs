using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.Vehicle.Contracts.v1.Repairs;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.Vehicle.Contracts.Permissions;

namespace AMIS.Modules.Vehicle.Features.v1.Repairs.SearchRepairRecords;

public static class SearchRepairRecordsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", async ([AsParameters] SearchRepairRecordsQuery query, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(query, ct)))
        .WithName(nameof(SearchRepairRecordsQuery))
        .WithSummary("Search repair records â€” filter by vehicle, status, date range, keyword")
        .Produces<PagedResponse<RepairRecordDto>>()
        .RequirePermission(VehiclePermissions.Repairs.View);
}

