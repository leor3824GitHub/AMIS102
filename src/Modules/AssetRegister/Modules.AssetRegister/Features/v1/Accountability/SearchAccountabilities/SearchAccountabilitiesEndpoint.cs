using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.SearchAccountabilities;

public static class SearchAccountabilitiesEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", Handle)
            .WithModuleName<SearchAccountabilitiesQuery>()
            .WithSummary("Search accountabilities (ICS / PAR)")
            .Produces<PagedResponse<PropertyAccountabilitySummaryDto>>()
            .RequirePermission(AssetRegisterModuleConstants.Permissions.Accountability.View);

    private static async Task<IResult> Handle(
        IMediator mediator,
        string? keyword = null,
        AccountabilityType? type = null,
        AccountabilityStatus? status = null,
        Guid? receivedByEmployeeId = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new SearchAccountabilitiesQuery(
            keyword, type, status, receivedByEmployeeId, fromDate, toDate, pageNumber, pageSize), ct);
        return TypedResults.Ok(result);
    }
}

