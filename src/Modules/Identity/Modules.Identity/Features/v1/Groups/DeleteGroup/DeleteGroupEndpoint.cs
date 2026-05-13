using AMIS.Framework.Shared.Identity;
using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Identity.Contracts.v1.Groups.DeleteGroup;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Identity.Features.v1.Groups.DeleteGroup;

public static class DeleteGroupEndpoint
{
    public static RouteHandlerBuilder MapDeleteGroupEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/groups/{id:guid}", (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
            mediator.Send(new DeleteGroupCommand(id), cancellationToken))
        .WithName("DeleteGroup")
        .WithSummary("Delete a group")
        .RequirePermission(IdentityPermissionConstants.Groups.Delete)
        .WithDescription("Soft delete a group. System groups cannot be deleted.");
    }
}

