using AMIS.Modules.Chat.Contracts.Permissions;
using AMIS.Modules.Chat.Contracts.v1.Channels;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Chat.Features.v1.Channels.ArchiveChannel;

internal static class ArchiveChannelEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapDelete("/channels/{channelId:guid}", ArchiveChannel)
            .WithName("Chat_ArchiveChannel")
            .WithSummary("Archive (soft-delete) a channel (channel admins only)")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ChatPermissions.Channels.Create);

    private static async Task<IResult> ArchiveChannel(
        Guid channelId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new ArchiveChannelCommand(channelId), cancellationToken);
        return TypedResults.NoContent();
    }
}
