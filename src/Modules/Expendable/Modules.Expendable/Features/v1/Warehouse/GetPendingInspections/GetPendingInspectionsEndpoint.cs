using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Persistence;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse.GetPendingInspections;

public static class GetPendingInspectionsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/inspections/pending", GetPending)
            .WithName(nameof(GetPendingInspectionsQuery))
            .WithSummary("Get pending inspections")
            .Produces<PagedResponse<PurchaseInspectionDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(ExpendableModuleConstants.Permissions.Inventory.ViewReports);

    private static async Task<IResult> GetPending(
        GetPendingInspectionsQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}


