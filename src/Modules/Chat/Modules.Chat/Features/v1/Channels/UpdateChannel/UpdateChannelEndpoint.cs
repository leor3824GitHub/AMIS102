using AMIS.Modules.Chat.Contracts.Permissions;
using AMIS.Modules.Chat.Contracts.v1.Channels;
using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Chat.Features.v1.Channels.UpdateChannel;

internal static class UpdateChannelEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/channels/{channelId:guid}", UpdateChannel)
            .WithName("Chat_UpdateChannel")
            .WithSummary("Rename or re-topic a channel (channel admins only)")
            .Produces<ChannelDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ChatPermissions.Channels.Create);

    private static async Task<IResult> UpdateChannel(
        Guid channelId,
        UpdateChannelRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateChannelCommand(channelId, request.Name, request.Topic), cancellationToken);
        return TypedResults.Ok(result);
    }

    /// <summary>Body for <see cref="UpdateChannelEndpoint"/>; the channel id comes from the route.</summary>
    public sealed record UpdateChannelRequest(string Name, string? Topic = null);
}
