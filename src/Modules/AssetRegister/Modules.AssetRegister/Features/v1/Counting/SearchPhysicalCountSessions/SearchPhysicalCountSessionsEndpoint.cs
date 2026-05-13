using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.AssetRegister.Contracts.v1;
using FSH.Modules.AssetRegister.Contracts.v1.Counting;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetRegister.Features.v1.Counting.SearchPhysicalCountSessions;

public static class SearchPhysicalCountSessionsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", Handle)
            .WithName(nameof(SearchPhysicalCountSessionsQuery))
            .WithSummary("Search physical count sessions")
            .Produces<PagedResponse<PhysicalCountSessionSummaryDto>>()
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Count.View);

    private static async Task<IResult> Handle(
        IMediator mediator,
        string? keyword = null,
        PhysicalCountStatus? status = null,
        PhysicalCountScope? scope = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new SearchPhysicalCountSessionsQuery(
            keyword, status, scope, fromDate, toDate, pageNumber, pageSize), ct);
        return TypedResults.Ok(result);
    }
}
