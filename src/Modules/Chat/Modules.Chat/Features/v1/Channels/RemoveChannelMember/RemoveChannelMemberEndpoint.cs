using AMIS.Modules.Chat.Contracts.Permissions;
using AMIS.Modules.Chat.Contracts.v1.Channels;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Chat.Features.v1.Channels.RemoveChannelMember;

internal static class RemoveChannelMemberEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapDelete("/channels/{channelId:guid}/members/{userId}", RemoveChannelMember)
            .WithName("Chat_RemoveChannelMember")
            .WithSummary("Remove a member from a channel (channel admins only)")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ChatPermissions.Channels.Create);

    private static async Task<IResult> RemoveChannelMember(
        Guid channelId,
        string userId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new RemoveChannelMemberCommand(channelId, userId), cancellationToken);
        return TypedResults.NoContent();
    }
}
