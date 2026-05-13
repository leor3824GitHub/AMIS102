using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Persistence;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Requests.GetEmployeeSupplyRequests;

public static class GetEmployeeSupplyRequestsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/employee/{employeeId}/requests", GetByEmployee)
            .WithName(nameof(GetEmployeeSupplyRequestsQuery))
            .WithSummary("Get employee supply requests")
            .Produces<PagedResponse<SupplyRequestDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(ExpendableModuleConstants.Permissions.SupplyRequests.View);

    private static async Task<IResult> GetByEmployee(
        string employeeId,
        [AsParameters] GetEmployeeSupplyRequestsQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var q = query with { EmployeeId = employeeId };
        var result = await mediator.Send(q, cancellationToken);
        return TypedResults.Ok(result);
    }
}


