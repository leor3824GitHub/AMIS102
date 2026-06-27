using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Notifications.Contracts.Permissions;
using AMIS.Modules.Notifications.Contracts.v1.Notifications;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Notifications.Features.v1.MarkAllRead;

internal static class MarkAllNotificationsReadEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/read-all", MarkAllRead)
            .WithName("Notifications_MarkAllNotificationsRead")
            .WithSummary("Mark all of the current user's notifications read")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission(NotificationPermissions.Notifications.View);

    private static async Task<IResult> MarkAllRead(IMediator mediator, CancellationToken cancellationToken)
    {
        await mediator.Send(new MarkAllNotificationsReadCommand(), cancellationToken);
        return TypedResults.NoContent();
    }
}
