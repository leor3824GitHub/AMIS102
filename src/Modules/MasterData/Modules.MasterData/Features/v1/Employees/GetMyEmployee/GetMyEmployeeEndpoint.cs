using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MasterData.Features.v1.Employees.GetMyEmployee;

public static class GetMyEmployeeEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/me", GetMyEmployee)
            .WithName(nameof(GetMyEmployeeQuery))
            .WithSummary("Get my employee profile")
            .Produces<MyEmployeeDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(MasterDataModuleConstants.Permissions.Employees.View);

    private static async Task<IResult> GetMyEmployee(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMyEmployeeQuery(), cancellationToken);
        return TypedResults.Ok(result);
    }
}
