using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Notifications.Contracts.Permissions;
using AMIS.Modules.Notifications.Contracts.v1.Notifications;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Notifications.Features.v1.GetUnreadCount;

internal static class GetUnreadCountEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/unread-count", GetUnreadCount)
            .WithName("Notifications_GetUnreadCount")
            .WithSummary("Count the current user's unread notifications (bell badge)")
            .Produces<int>(StatusCodes.Status200OK)
            .RequirePermission(NotificationPermissions.Notifications.View);

    private static async Task<IResult> GetUnreadCount(IMediator mediator, CancellationToken cancellationToken)
    {
        var count = await mediator.Send(new GetUnreadCountQuery(), cancellationToken);
        return TypedResults.Ok(count);
    }
}
