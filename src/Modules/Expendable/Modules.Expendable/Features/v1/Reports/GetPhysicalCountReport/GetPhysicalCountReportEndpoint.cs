using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Reports.GetPhysicalCountReport;

public static class GetPhysicalCountReportEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/physical-count", GetReport)
            .WithName("Expendable_GetPhysicalCountReport")
            .WithSummary("Physical count report listing all products with system inventory balances")
            .Produces<List<PhysicalCountItemDto>>(StatusCodes.Status200OK)
            .RequirePermission(ExpendableModuleConstants.Permissions.Inventory.ViewReports);

    private static async Task<IResult> GetReport(
        [AsParameters] GetPhysicalCountReportQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}

