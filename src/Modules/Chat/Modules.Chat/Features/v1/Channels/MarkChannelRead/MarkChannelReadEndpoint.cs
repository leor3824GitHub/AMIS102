using AMIS.Modules.Chat.Contracts.Permissions;
using AMIS.Modules.Chat.Contracts.v1.Channels;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Chat.Features.v1.Channels.MarkChannelRead;

internal static class MarkChannelReadEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/channels/{channelId:guid}/read", MarkChannelRead)
            .WithName("Chat_MarkChannelRead")
            .WithSummary("Mark a channel read up to a given message for the caller")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ChatPermissions.Channels.View);

    private static async Task<IResult> MarkChannelRead(
        Guid channelId,
        MarkChannelReadRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new MarkChannelReadCommand(channelId, request.MessageId), cancellationToken);
        return TypedResults.NoContent();
    }

    /// <summary>Body for <see cref="MarkChannelReadEndpoint"/>; the channel id comes from the route.</summary>
    public sealed record MarkChannelReadRequest(Guid MessageId);
}
