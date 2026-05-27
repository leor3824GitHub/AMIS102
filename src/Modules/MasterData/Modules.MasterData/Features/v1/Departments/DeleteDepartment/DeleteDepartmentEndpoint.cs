using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.MasterData.Contracts.v1.References;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.MasterData.Contracts.Permissions;

namespace AMIS.Modules.MasterData.Features.v1.Departments.DeleteDepartment;

public static class DeleteDepartmentEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapDelete("/{id:guid}", DeleteDepartment)
            .WithName(nameof(DeleteDepartmentCommand))
            .WithSummary("Delete department")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(MasterDataPermissions.Departments.Delete);

    private static async Task<IResult> DeleteDepartment(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteDepartmentCommand(id), cancellationToken);
        return TypedResults.NoContent();
    }
}
