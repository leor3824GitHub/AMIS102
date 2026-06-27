using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Notifications.Contracts.Permissions;
using AMIS.Modules.Notifications.Contracts.v1.Notifications;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Notifications.Features.v1.DismissNotification;

internal static class DismissNotificationEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapDelete("/{id:guid}", Dismiss)
            .WithName("Notifications_DismissNotification")
            .WithSummary("Dismiss (delete) one of the current user's notifications")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(NotificationPermissions.Notifications.View);

    private static async Task<IResult> Dismiss(Guid id, IMediator mediator, CancellationToken cancellationToken)
    {
        await mediator.Send(new DismissNotificationCommand(id), cancellationToken);
        return TypedResults.NoContent();
    }
}
