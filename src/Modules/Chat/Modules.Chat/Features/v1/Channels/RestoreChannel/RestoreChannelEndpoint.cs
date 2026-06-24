using AMIS.Modules.Chat.Contracts.Permissions;
using AMIS.Modules.Chat.Contracts.v1.Channels;
using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Chat.Features.v1.Channels.RestoreChannel;

internal static class RestoreChannelEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/channels/{channelId:guid}/restore", RestoreChannel)
            .WithName("Chat_RestoreChannel")
            .WithSummary("Restore an archived channel (channel admins only)")
            .Produces<ChannelDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ChatPermissions.Channels.Create);

    private static async Task<IResult> RestoreChannel(
        Guid channelId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RestoreChannelCommand(channelId), cancellationToken);
        return TypedResults.Ok(result);
    }
}
