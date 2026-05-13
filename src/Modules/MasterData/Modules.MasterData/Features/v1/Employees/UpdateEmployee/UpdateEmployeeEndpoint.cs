using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.MasterData.Contracts.v1.References;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.MasterData.Features.v1.Employees.UpdateEmployee;

public static class UpdateEmployeeEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}", UpdateEmployee)
            .WithName(nameof(UpdateEmployeeCommand))
            .WithSummary("Update employee")
            .Produces<EmployeeReferenceDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(MasterDataModuleConstants.Permissions.Employees.Update);

    private static async Task<IResult> UpdateEmployee(
        Guid id,
        UpdateEmployeeCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var cmd = command with { Id = id };
        var result = await mediator.Send(cmd, cancellationToken);
        return TypedResults.Ok(result);
    }
}
