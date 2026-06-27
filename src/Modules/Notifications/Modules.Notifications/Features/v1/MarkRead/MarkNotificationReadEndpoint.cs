using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Notifications.Contracts.Permissions;
using AMIS.Modules.Notifications.Contracts.v1.Notifications;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Notifications.Features.v1.MarkRead;

internal static class MarkNotificationReadEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/read", MarkRead)
            .WithName("Notifications_MarkNotificationRead")
            .WithSummary("Mark one of the current user's notifications read")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(NotificationPermissions.Notifications.View);

    private static async Task<IResult> MarkRead(Guid id, IMediator mediator, CancellationToken cancellationToken)
    {
        await mediator.Send(new MarkNotificationReadCommand(id), cancellationToken);
        return TypedResults.NoContent();
    }
}
