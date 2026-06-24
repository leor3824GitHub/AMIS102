using AMIS.Modules.Chat.Contracts.Permissions;
using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Modules.Chat.Contracts.v1.Messages;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Chat.Features.v1.Messages.GetPinnedMessages;

internal static class GetPinnedMessagesEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/channels/{channelId:guid}/pinned", GetPinnedMessages)
            .WithName("Chat_GetPinnedMessages")
            .WithSummary("List a channel's pinned messages (most recently pinned first)")
            .Produces<IReadOnlyList<MessageDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ChatPermissions.Channels.View);

    private static async Task<IResult> GetPinnedMessages(
        Guid channelId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPinnedMessagesQuery(channelId), cancellationToken);
        return TypedResults.Ok(result);
    }
}
