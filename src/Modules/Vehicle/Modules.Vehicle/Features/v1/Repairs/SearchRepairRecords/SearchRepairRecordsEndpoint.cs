using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Vehicle.Contracts.v1.Repairs;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Vehicle.Features.v1.Repairs.SearchRepairRecords;

public static class SearchRepairRecordsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", async ([AsParameters] SearchRepairRecordsQuery query, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(query, ct)))
        .WithName(nameof(SearchRepairRecordsQuery))
        .WithSummary("Search repair records — filter by vehicle, status, date range, keyword")
        .Produces<PagedResponse<RepairRecordDto>>()
        .RequirePermission(VehicleModuleConstants.Permissions.Repairs.View);
}
