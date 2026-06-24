using AMIS.Modules.Chat.Contracts.Permissions;
using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Modules.Chat.Contracts.v1.Messages;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Chat.Features.v1.Messages.SearchChannelMessages;

internal static class SearchChannelMessagesEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/channels/{channelId:guid}/messages/search", SearchChannelMessages)
            .WithName("Chat_SearchChannelMessages")
            .WithSummary("Search a channel's messages by content (ILIKE, keyset paginated, newest first)")
            .Produces<MessagePageDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ChatPermissions.Channels.View);

    private static async Task<IResult> SearchChannelMessages(
        Guid channelId,
        string? q,
        Guid? before,
        int? take,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SearchChannelMessagesQuery(channelId, q ?? string.Empty, before, take), cancellationToken);
        return TypedResults.Ok(result);
    }
}
