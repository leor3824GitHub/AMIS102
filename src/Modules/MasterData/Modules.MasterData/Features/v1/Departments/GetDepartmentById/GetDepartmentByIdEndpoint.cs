using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.MasterData.Contracts.v1.References;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.MasterData.Contracts.Permissions;

namespace AMIS.Modules.MasterData.Features.v1.Departments.GetDepartmentById;

public static class GetDepartmentByIdEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", GetDepartmentById)
            .WithName(nameof(GetDepartmentReferenceByIdQuery))
            .WithSummary("Get department by id")
            .Produces<DepartmentReferenceDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(MasterDataPermissions.Departments.View);

    private static async Task<IResult> GetDepartmentById(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetDepartmentReferenceByIdQuery(id), cancellationToken);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }
}
