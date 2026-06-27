using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Notifications.Contracts.Permissions;
using AMIS.Modules.Notifications.Contracts.v1.DTOs;
using AMIS.Modules.Notifications.Contracts.v1.Notifications;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Notifications.Features.v1.ListMyNotifications;

internal static class ListMyNotificationsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", ListMyNotifications)
            .WithName("Notifications_ListMyNotifications")
            .WithSummary("List the current user's notifications, newest first")
            .Produces<IReadOnlyList<NotificationDto>>(StatusCodes.Status200OK)
            .RequirePermission(NotificationPermissions.Notifications.View);

    private static async Task<IResult> ListMyNotifications(
        [AsParameters] ListMyNotificationsRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListMyNotificationsQuery(request.UnreadOnly, request.Take), cancellationToken);
        return TypedResults.Ok(result);
    }

    /// <summary>Query string for <see cref="ListMyNotificationsEndpoint"/>.</summary>
    public sealed record ListMyNotificationsRequest(bool UnreadOnly = false, int Take = 50);
}
